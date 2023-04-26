using Fody;
using MessagePack;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using ShareAttributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PolymorphicMessagePack.Fody
{
    public class MsgPackPolyWeaver : BaseModuleWeaver
    {
        public override bool ShouldCleanReference => true;

        Type _absAttr = typeof(UnionAbsOrInterfaceAttribute);
        Type _genericUnion = typeof(GenericUnionAttribute);
        Type _reqUnionAttr = typeof(RequireUnionAttribute);
        Type _reqGenericUnionAttr = typeof(RequireUnionGenericAttribute);

        Type _msgObj = typeof(MessagePackObjectAttribute);
        Type _msgKey = typeof(KeyAttribute);

        string _polyAttrAsName;
        string _msgPackAttrAsName;

        TypeDefinition _objectTypeRef;

        TypeDefinition _requireUnionAttrRef;
        MethodReference _requireUnionAttrConstructor;

        TypeDefinition _requireUnionGenericAttrRef;
        MethodReference _requireUnionGenericAttrConstructor;

        TypeDefinition _keyMarkRef;

        public MsgPackPolyWeaver()
        {
            _polyAttrAsName = _absAttr.Assembly.GetName().Name;
            _msgPackAttrAsName=_msgObj.Assembly.GetName().Name;
        }

        public override void Execute()
        {
            var ns = GetValueFromConfig("NameSpace");
            if(ns == null)
            {
                WriteError("Scan Assembly name not set in config");
                return;
            }
            InitBasicRequireRef();

            var requireConsiderAbsAndInterfaceTypes = ModuleDefinition.Types.Where(
                x => x.HasCustomAttributes &&
                x.Namespace == ns &&
                x.CustomAttributes.Any(y => y.AttributeType.FullName == _absAttr.FullName));

            var resultsForNonGenericTypes = GetNonGenericDerivedTypes(ModuleDefinition, requireConsiderAbsAndInterfaceTypes).OrderBy(x => x.FullName);
            
            var resultsForGenericTypes = GetGenericDerivedTypes(ModuleDefinition, requireConsiderAbsAndInterfaceTypes).OrderBy(x => x.Item1.FullName);

            //pass nongeneric types
            HashSet<TypeDefinition> ignoreNonGenericTypes=new HashSet<TypeDefinition>();
            //record all manual marked used static ids
            Dictionary<uint, TypeDefinition> manualMarkUsedIdForNonGenericTypes = new Dictionary<uint, TypeDefinition>();

            Dictionary<uint, (TypeDefinition, CustomAttribute)> manualMarkUsedIdForGenericTypes = new Dictionary<uint, (TypeDefinition, CustomAttribute)>();
            
            //check non generic first
            foreach(var derivedType in resultsForNonGenericTypes)
            {
                if(derivedType.HasCustomAttributes &&
                   derivedType.CustomAttributes.Any(x => x.AttributeType.FullName == _reqGenericUnionAttr.FullName || x.AttributeType.FullName==_genericUnion.FullName))
                {
                    WriteError($"Error: {derivedType.FullName} set generic types(this is non generic type)");
                    return;
                }
                //If Marked [RequireUnionAttribute] by manual
                if (derivedType.HasCustomAttributes && 
                    derivedType.CustomAttributes.Any(x=>x.AttributeType.FullName== _reqUnionAttr.FullName))
                {
                    var markedReqUnionAttr=derivedType.CustomAttributes.Single(x=>x.AttributeType.FullName==_reqUnionAttr.FullName);

                    //public RequireUnionAttribute(uint unionUniqueId)
                    var manualSetId = (uint)markedReqUnionAttr.ConstructorArguments[0].Value;

                    if (manualMarkUsedIdForNonGenericTypes.TryGetValue(manualSetId,out var existUsedIdType))
                    {
                        WriteError($"Error: {manualSetId} set for diff types:{derivedType.FullName} and {existUsedIdType.FullName}");
                        return;
                    }
                    manualMarkUsedIdForNonGenericTypes.Add(manualSetId, derivedType);
                    ignoreNonGenericTypes.Add(derivedType);
                }
            }

            foreach(var derivedGenericType in resultsForGenericTypes)
            {
                var classtyperef = derivedGenericType.Item1;

                var relateAttributes = derivedGenericType.Item2;

                if (classtyperef.HasCustomAttributes &&
                    classtyperef.CustomAttributes.Any(x => x.AttributeType.FullName == _reqUnionAttr.FullName))
                {
                    WriteError($"Error: {classtyperef.FullName} set non generic types(this is generic type)");
                    return;
                }

                //generic type really diff to compare
                //maybe mark with same attr
                //e.g:
                //[RequireUnionGenericAttribute(1,typeof(string))]
                //[RequireUnionGenericAttribute(2,typeof(string))]
                //[RequireUnionGenericAttribute(3,typeof(int))]
                //[RequireUnionGenericAttribute(4,typeof(int))]
                //forget it
                //visit classtyperef attributes

                //public RequireUnionGenericAttribute(uint unionUniqueId, Type supportGeneric)
                if (classtyperef.HasCustomAttributes &&
                    classtyperef.CustomAttributes.Any(x => x.AttributeType.FullName == _requireUnionGenericAttrRef.FullName))
                {
                    //using type to group
                    var groupsTypeAttributes = classtyperef.CustomAttributes
                        .Where(x => x.AttributeType.FullName == _requireUnionGenericAttrRef.FullName)
                        .GroupBy(y => y.ConstructorArguments[1].Value.ToString());
                    foreach(var groupAttr in groupsTypeAttributes)
                    {
                        //only one mark
                        if (groupAttr.Count()==1)
                        {
                            var targetAttr = groupAttr.First();

                            var manualSetId = (uint)targetAttr.ConstructorArguments[0].Value;
                            var typeName = targetAttr.ConstructorArguments[1].Value.ToString();

                            var setType = (TypeReference)targetAttr.ConstructorArguments[1].Value;
                            var setTypeDef = setType.Resolve();

                            if (!setType.IsGenericInstance || setTypeDef.FullName!=classtyperef.FullName)
                            {
                                WriteError($"{classtyperef.FullName} fixed id manaully,but target union type not {classtyperef.FullName} generic type (is {setTypeDef.FullName})");
                                return;
                            }

                            if (manualMarkUsedIdForNonGenericTypes.TryGetValue(manualSetId, out var _) 
                                || manualMarkUsedIdForGenericTypes.TryGetValue(manualSetId, out var _))
                            {
                                string outputString;
                                var typeForNonGeneric = manualMarkUsedIdForNonGenericTypes.TryGetValue(manualSetId, out var existNonType);
                                var typeForGeneric = manualMarkUsedIdForGenericTypes.TryGetValue(manualSetId, out var existTypeWithGeneric);
                                if (typeForNonGeneric)
                                    outputString = existNonType.FullName;
                                else
                                    outputString = $"{existTypeWithGeneric.Item1.FullName}-{existTypeWithGeneric.Item2.ConstructorArguments[1].Value}";
                                WriteError($"Error: {manualSetId} set for diff types:{classtyperef.FullName}-{typeName} and {outputString}");
                                return;
                            }

                            manualMarkUsedIdForGenericTypes.Add(manualSetId, (classtyperef, targetAttr));
                            //remove same type mark attr in attributes
                            //remove from class
                            var existOldAttributes = classtyperef.CustomAttributes.Where(x =>
                                                        x.AttributeType.FullName==_genericUnion.FullName &&
                                                        x.ConstructorArguments[0].Value.ToString() == typeName).ToList();
                            foreach (var oldAttribute in existOldAttributes)
                                classtyperef.CustomAttributes.Remove(oldAttribute);

                            //remove from auto generate attr
                            existOldAttributes = relateAttributes.Where(x =>
                                                        x.AttributeType.FullName == _genericUnion.FullName &&
                                                        x.ConstructorArguments[0].Value.ToString() == typeName).ToList();
                            foreach (var oldAttribute in existOldAttributes)
                                relateAttributes.Remove(oldAttribute);
                        }
                        //multi manual set 
                        else
                        {
                            WriteError($"Error: {classtyperef.FullName}-{groupAttr.Key} has more than one mark");
                            return;
                        }
                    }
                }
                
            }

            uint autoIncreaseIdForPoly = 1;
            while (manualMarkUsedIdForNonGenericTypes.ContainsKey(autoIncreaseIdForPoly) || manualMarkUsedIdForGenericTypes.ContainsKey(autoIncreaseIdForPoly))
                autoIncreaseIdForPoly++;

            //Add [RequireUnion(uint x)] into target class which from marked abs/interface and marked [MessageObject]
            foreach (var derivedType in resultsForNonGenericTypes)
            {
                if (ignoreNonGenericTypes.Contains(derivedType))
                    continue;

                var attribute = new CustomAttribute(_requireUnionAttrConstructor);
                attribute.ConstructorArguments.Add(
                    new CustomAttributeArgument(
                        _requireUnionAttrConstructor.Parameters[0].ParameterType,
                        autoIncreaseIdForPoly));
                derivedType.CustomAttributes.Add(attribute);
                autoIncreaseIdForPoly++;
                while (manualMarkUsedIdForNonGenericTypes.ContainsKey(autoIncreaseIdForPoly) || manualMarkUsedIdForGenericTypes.ContainsKey(autoIncreaseIdForPoly))
                    autoIncreaseIdForPoly++;
            }

            //Add [RequireUnionGeneric(uint x,Type y)] into target class which from marked abs/interface and marked [MessageObject]
            foreach (var derivedType in resultsForGenericTypes)
            {
                var classtyperef = derivedType.Item1;
                var relateAttributes = derivedType.Item2.OrderBy(x => x.ConstructorArguments[0].Value.ToString());
                foreach (var defineoldAttribute in relateAttributes)
                {
                    //using marked class type to package generic
                    var newClassdefWithGeneric = new GenericInstanceType(classtyperef);
                    newClassdefWithGeneric.GenericArguments.Add(defineoldAttribute.ConstructorArguments[0].Value as TypeReference);

                    var attribute = new CustomAttribute(_requireUnionGenericAttrConstructor);

                    attribute.ConstructorArguments.Add(new CustomAttributeArgument(_requireUnionGenericAttrConstructor.Parameters[0].ParameterType, autoIncreaseIdForPoly));
                    attribute.ConstructorArguments.Add(new CustomAttributeArgument(_requireUnionGenericAttrConstructor.Parameters[1].ParameterType, newClassdefWithGeneric));

                    var existOldAttributes = classtyperef.CustomAttributes.Where(x => x.ConstructorArguments[0].Value.ToString() == defineoldAttribute.ConstructorArguments[0].Value.ToString()).ToList();
                    foreach (var oldAttribute in existOldAttributes)
                        classtyperef.CustomAttributes.Remove(oldAttribute);

                    classtyperef.CustomAttributes.Add(attribute);

                    autoIncreaseIdForPoly++;
                    while (manualMarkUsedIdForNonGenericTypes.ContainsKey(autoIncreaseIdForPoly) || manualMarkUsedIdForGenericTypes.ContainsKey(autoIncreaseIdForPoly))
                        autoIncreaseIdForPoly++;
                }
            }
            WriteInfo($"Generate Auto PolyMark Run Finished.(Used for {ns})");
        }

        private List<TypeDefinition> GetNonGenericDerivedTypes(
            ModuleDefinition module, 
            IEnumerable<TypeDefinition> baseTypes)
        {
            //get all non generic class types
            //also must has [MessagePackObject]
            var classdefs = module.GetTypes().
                Where(x => 
                !x.IsAbstract && 
                x.IsClass && 
                !x.HasGenericParameters &&
                x.HasCustomAttributes &&
                x.CustomAttributes.Any(y => y.AttributeType.FullName == _msgObj.FullName));

            List<TypeDefinition> derivedtypes = new List<TypeDefinition>();

            foreach (var classdef in classdefs)
            {
                if (derivedtypes.Contains(classdef))
                    continue;

                var nextcheck = classdef;

                while (nextcheck != null && nextcheck.FullName != _objectTypeRef.FullName)
                {
                    var baseType = baseTypes.Where(x => x.IsAbstract && x.FullName == nextcheck.FullName).FirstOrDefault();
                    if (baseType != null)
                    {
                        derivedtypes.Add(classdef);
                        break;
                    }

                    nextcheck = nextcheck.BaseType?.Resolve();
                }

                if (!derivedtypes.Contains(classdef))
                {
                    var relateInterfaces = baseTypes.Where(x => x.IsInterface && classdef.Interfaces.Any(y => y.InterfaceType.FullName == x.FullName));
                    if (relateInterfaces.Count() > 0)
                    {
                        derivedtypes.Add(classdef);
                    }
                }
            }
            return derivedtypes;
        }

        private List<(TypeDefinition, List<CustomAttribute>)> GetGenericDerivedTypes(
            ModuleDefinition module, 
            IEnumerable<TypeDefinition> baseTypes)
        {
            var classdefs = module.GetTypes().
                Where(x =>
                !x.IsAbstract &&
                x.IsClass &&
                x.HasGenericParameters &&
                x.HasCustomAttributes &&
                x.CustomAttributes.Any(y => y.AttributeType.FullName == _msgObj.FullName));

            List<(TypeDefinition, List<CustomAttribute>)> derivedtypes = new List<(TypeDefinition, List<CustomAttribute>)>();

            foreach (var classdef in classdefs)
            {
                //avoid same generic type repeat attr
                var genericUnionTypes = classdef.CustomAttributes
                    .Where(x => x.AttributeType.FullName == _genericUnion.FullName)
                    .ToLookup(y => y.ConstructorArguments[0].Value.ToString())
                    .Select(z => z.First()).ToList();

                //package
                var package = (classdef, genericUnionTypes);

                var nextcheck = classdef;
                bool isInAnyAbs = false;
                while (nextcheck != null && nextcheck.FullName != _objectTypeRef.FullName)
                {
                    var baseType = baseTypes.Where(x => x.IsAbstract && x.FullName == nextcheck.FullName).FirstOrDefault();
                    if (baseType != null)
                    {
                        derivedtypes.Add(package);
                        isInAnyAbs = true;
                        break;
                    }
                    nextcheck = nextcheck.BaseType?.Resolve();
                }
                if (!isInAnyAbs)
                {
                    //check all interface base type
                    var relateInterfaces = baseTypes.Where(x => x.IsInterface && classdef.Interfaces.Any(y => (y.InterfaceType.IsGenericInstance && y.InterfaceType.GetElementType().FullName == x.FullName) ||
                    (!y.InterfaceType.IsGenericInstance && y.InterfaceType.FullName == x.FullName)));
                    if (relateInterfaces.Count() > 0)
                    {
                        derivedtypes.Add(package);
                    }
                }

            }
            return derivedtypes;
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
            yield return "System";
            yield return "System.Runtime";
            yield return "System.Core";
        }

        void InitBasicRequireRef()
        {
            _objectTypeRef = ModuleDefinition.ImportReference(TypeSystem.ObjectDefinition).Resolve();
            var msgPackAssembly = new AssemblyNameReference(_msgPackAttrAsName, null);
            var polyAttrAssembly = new AssemblyNameReference(_polyAttrAsName, null);
            try
            {
                _requireUnionAttrRef = ModuleDefinition.AssemblyResolver.Resolve(polyAttrAssembly).
                    MainModule.Types.
                    Single(type => type.Name == _reqUnionAttr.Name);
                _requireUnionGenericAttrRef = ModuleDefinition.AssemblyResolver.Resolve(polyAttrAssembly).
                    MainModule.Types.
                    Single(type => type.Name == _reqGenericUnionAttr.Name);
                _keyMarkRef = ModuleDefinition.AssemblyResolver.Resolve(msgPackAssembly).
                    MainModule.Types.
                    Single(type => type.Name == _msgKey.Name);

                var requireUnionAttrConstructor= _requireUnionAttrRef
                                                .GetConstructors()
                                                .Single(ctor => 
                                                    1 == ctor.Parameters.Count &&
                                                    "System.UInt32" == ctor.Parameters[0].ParameterType.FullName);
                _requireUnionAttrConstructor = ModuleDefinition.ImportReference(requireUnionAttrConstructor);

                var requireUnionGenericAttrConstructor=_requireUnionGenericAttrRef
                                                .GetConstructors()
                                                .Single(ctor => 
                                                    2 == ctor.Parameters.Count &&
                                                    "System.UInt32" == ctor.Parameters[0].ParameterType.FullName &&
                                                    "System.Type" == ctor.Parameters[1].ParameterType.FullName);
                _requireUnionGenericAttrConstructor=ModuleDefinition.ImportReference(requireUnionGenericAttrConstructor);

            }
            catch (Exception ex)
            {
                WriteError($"Init Basic TypeDefine Failed ({ex.Message})");
            }
        }

        #region GetAssemblyNameSpaceAttributes
        string GetValueFromConfig(string name)
        {
            var attribute = Config?.Attribute(name);
            if (attribute == null)
            {
                return null;
            }

            var value = attribute.Value;
            return value;
        }


        #endregion
    }
}
