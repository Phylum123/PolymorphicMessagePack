using Fody;
using MessagePack;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace PolymorphicMessagePack.Fody
{
    public class AutoMsgPackKeyWeaver : BaseModuleWeaver
    {
        public override bool ShouldCleanReference => true;

        //require check
        Type _msgObjAttr = typeof(MessagePackObjectAttribute);
        Type _msgDataContractAttr = typeof(DataContractAttribute);

        //ignore prop
        Type _msgIgnoreAttr = typeof(IgnoreMemberAttribute);
        Type _msgDataIgnoreAttr = typeof(IgnoreDataMemberAttribute);

        //mark key
        Type _msgKeyAttr = typeof(KeyAttribute);
        Type _msgDataMemberAttr = typeof(DataMemberAttribute);

        string _msgPackAttrAsName;

        //object for stop while
        TypeDefinition _objectTypeDef;
        //Auto property created private field attribute
        TypeDefinition _autoPropFieldAttrDef;
        //key mark def
        TypeDefinition _keyMarkDef;

        TypeDefinition _msgIgnoreDef;
        TypeDefinition _msgDataIgnoreDef;

        MethodReference _msgIgnoreConstructor;

        MethodReference _msgKeyConstructor;
        internal class ReferenceTreeNode
        {
            internal TypeDefinition node;
            internal HashSet<int> ManualMarkedIdKeys = new HashSet<int>();
            internal HashSet<int> CurrentTypeOwnIdKeys= new HashSet<int>();
            internal List<FieldDefinition> RequireAddKeyFields=new List<FieldDefinition>();
            internal List<PropertyDefinition> RequireAddKeyProperties=new List<PropertyDefinition>();
            internal ReferenceTreeNode Parent;
            internal List<ReferenceTreeNode> Childs=new List<ReferenceTreeNode>();
        }

        public AutoMsgPackKeyWeaver()
        {
            _msgPackAttrAsName = _msgObjAttr.Assembly.GetName().Name;
        }

        public override void Execute()
        {
            var ns = GetStringFromConfig("NameSpace");
            if (ns == null)
            {
                WriteError("Scan Assembly name not set in config");
                return;
            }
            var markPrivateField = GetBoolFromConfig("AlsoMarkPrivateField");
            var markBaseTypeIgnoreMem = GetBoolFromConfig("MarkIgnoreToFieldForNonMsgPackBaseType");

            InitBasicRequireRef();

            //get all mark with [MessageObject] class
            var requireConsiderAbsAndInterfaceTypes = ModuleDefinition.Types.Where(
                x => x.HasCustomAttributes && //has attr
                x.Namespace == ns && // in target namespace
                !x.IsAbstract && //not abstract class
                !x.IsInterface && //not interface
                (x.IsClass || (x.IsValueType && !x.IsEnum && x.IsSealed)) && // is class or struct
                x.CustomAttributes.Any(y => 
                    y.AttributeType.FullName == _msgObjAttr.FullName ||
                    y.AttributeType.FullName== _msgDataContractAttr.FullName)); //has messageobject

            //check can mark
            foreach(var type in requireConsiderAbsAndInterfaceTypes)
            {
                if (!type.IsPublic)
                {
                    WriteError($"{type.FullName} not public type but marked [MessageObject] or [DataContract]");
                    return;
                }
            }

            var refTree = GetDerivedRelationprivate(ModuleDefinition, requireConsiderAbsAndInterfaceTypes);

            //scan each type,analysis each type own require marked fields,property,and check each manual set Key Ids
            foreach (var typeNode in refTree)
            {
                //internal:IsAssembly;private:IsPrivate;public:IsPublic
                var currentTypeRequireMarkCollection = GetNodeRequireMarkFieldAndProperties(typeNode.node, markPrivateField);
                //Scan and record manual set keys
                foreach (var field in currentTypeRequireMarkCollection.Item1)
                {
                    var keyAttrs = field.CustomAttributes.Where(x => x.AttributeType.FullName == _msgKeyAttr.FullName ||
                                                                   x.AttributeType.FullName == _msgDataMemberAttr.FullName);
                    if (keyAttrs.Count() > 1)
                    {
                        WriteError($"{typeNode.node.FullName}->Field:{field.Name} has both [Key] and [DataMember]");
                        return;
                    }
                    else if (keyAttrs.Count() == 1)
                    {
                        var checkedAttr = keyAttrs.First();
                        if (!CheckAndReigsterManualSetKeys(typeNode, checkedAttr))
                            return;
                    }
                    else
                    {
                        //not set attr
                        typeNode.RequireAddKeyFields.Add(field);
                    }
                }
                foreach (var property in currentTypeRequireMarkCollection.Item2)
                {
                    var keyAttrs = property.CustomAttributes.Where(x => x.AttributeType.FullName == _msgKeyAttr.FullName ||
                                                                   x.AttributeType.FullName == _msgDataMemberAttr.FullName);
                    if (keyAttrs.Count() > 1)
                    {
                        WriteError($"{typeNode.node.FullName}->Property:{property.Name} has both [Key] and [DataMember]");
                        return;
                    }
                    else if (keyAttrs.Count() == 1)
                    {
                        var checkedAttr = keyAttrs.First();
                        if (!CheckAndReigsterManualSetKeys(typeNode, checkedAttr))
                            return;
                    }
                    else
                    {
                        //not set attr
                        typeNode.RequireAddKeyProperties.Add(property);
                    }
                }

            }
            //each node get in that type which field and prop need mark,and get each type manual mark key ids
            //get all end node,check each ancestors used ids
            HashSet<ReferenceTreeNode> visitedNode= new HashSet<ReferenceTreeNode>();
            foreach(var typeNode in refTree.Where(x => x.Childs.Count == 0))
            {
                if (visitedNode.Contains(typeNode))
                    continue;
                visitedNode.Add(typeNode);
                var current = typeNode;
                var next = current.Parent;
                while (next != null)
                {
                    if (next.CurrentTypeOwnIdKeys.Overlaps(current.ManualMarkedIdKeys))
                    {
                        WriteError($"{current.node.FullName} has conflict key id with {next.node.FullName}");
                        return;
                    }
                    //attach current manual keys to parent manual keys
                    //let parent know can't generate key ids which derive type used
                    next.ManualMarkedIdKeys.UnionWith(current.ManualMarkedIdKeys);

                    current = next;
                    next=current.Parent;
                }
            }


            Stack<int> lastBaseTypeUsedId= new Stack<int>();
            Stack<List<ReferenceTreeNode>> requireVisitChildNodes = new Stack<List<ReferenceTreeNode>>();
            //Mark Key
            foreach(var typeNode in refTree.Where(x => x.Parent == null))
            {
                //each base type start with key id 0
                MarkTreeNode(typeNode, 0, markBaseTypeIgnoreMem);
            }
        }

        private void MarkTreeNode(ReferenceTreeNode root,int startId,bool markBaseTypeIgnoreMem)
        {
            //if config choose no [MessagePackObject] type set all fields to [ignoreMumber]
            if (markBaseTypeIgnoreMem && !root.node.CustomAttributes.Any(x=>x.AttributeType.FullName==_msgObjAttr.FullName))
            {
                foreach(var field in root.RequireAddKeyFields)
                {
                    var attribute = new CustomAttribute(_msgIgnoreConstructor);
                    field.CustomAttributes.Add(attribute);
                }
                foreach (var property in root.RequireAddKeyProperties)
                {
                    var attribute = new CustomAttribute(_msgIgnoreConstructor);
                    property.CustomAttributes.Add(attribute);
                }
            }
            else
            {
                MarkNodeFields(root, ref startId);
            }
            foreach(var deriveType in root.Childs)
            {
                MarkTreeNode(deriveType, startId, markBaseTypeIgnoreMem);
            }
        }
        private void MarkNodeFields(ReferenceTreeNode node,ref int startId)
        {
            foreach(var field in node.RequireAddKeyFields)
            {
                while (node.ManualMarkedIdKeys.Contains(startId))
                    startId++;

                var attribute = new CustomAttribute(_msgKeyConstructor);
                attribute.ConstructorArguments.Add(
                    new CustomAttributeArgument(
                        _msgKeyConstructor.Parameters[0].ParameterType,
                        startId));
                field.CustomAttributes.Add(attribute);
                startId++;
            }

            foreach(var property in node.RequireAddKeyProperties)
            {
                while (node.ManualMarkedIdKeys.Contains(startId))
                    startId++;

                var attribute = new CustomAttribute(_msgKeyConstructor);
                attribute.ConstructorArguments.Add(
                    new CustomAttributeArgument(
                        _msgKeyConstructor.Parameters[0].ParameterType,
                        startId));
                property.CustomAttributes.Add(attribute);
                startId++;
            }
        }

        private bool CheckAndReigsterManualSetKeys(ReferenceTreeNode node,CustomAttribute attribute)
        {
            int manualSetKey;
            //if is KeyAttribute and use KeyAttribute(int key)
            if (attribute.AttributeType.FullName == _msgKeyAttr.FullName &&
                attribute.ConstructorArguments[0].Type.FullName == TypeSystem.Int32Reference.FullName)
            {
                manualSetKey = (int)attribute.ConstructorArguments[0].Value;
            }
            //if is DataMemberAttribute and use DataMemberAttribute(Order=int)
            else if (attribute.AttributeType.FullName == _msgDataMemberAttr.FullName &&
                        attribute.Properties.Any(x => x.Name == "Order"))
            {
                manualSetKey = (int)attribute.Properties.Where(x => x.Name == "Order").First().Argument.Value;
            }
            //other set with name,not consider,continue
            else
            {
                return true;
            }
            //check if same key in one type
            if (node.ManualMarkedIdKeys.Contains(manualSetKey))
            {
                WriteError($"{node.node.FullName} has marked same key {manualSetKey} for more than one field or property");
                return false;
            }
            node.ManualMarkedIdKeys.Add(manualSetKey);
            node.CurrentTypeOwnIdKeys.Add(manualSetKey);
            return true;
        }

        private (List<FieldDefinition>,List<PropertyDefinition>) GetNodeRequireMarkFieldAndProperties(TypeDefinition targetType, bool markPrivateField)
        {
            List<FieldDefinition> considerFields = null;
            //ignore mark [IgnoreMember],[IgnoreDataMember]
            var query = targetType.Fields.Where(x => !x.CustomAttributes.Any(y =>
                        y.AttributeType.FullName == _msgIgnoreDef.FullName ||
                        y.AttributeType.FullName == _msgDataIgnoreDef.FullName));

            //make a copy
            if (markPrivateField)
                considerFields = query.ToList();
            else
                considerFields = query.Where(x => !(x.IsAssembly || x.IsPrivate) ||
                                x.CustomAttributes.Any(y => y.AttributeType.FullName == _autoPropFieldAttrDef.FullName)).ToList();

            var considerProperties = new List<PropertyDefinition>();
            //Auto Field ignore :{<ST1>k__BackingField}+{System.Runtime.CompilerServices.CompilerGeneratedAttribute}
            //Auto Field Match: <prop.Name>k__BackingField 

            //only consider auto-prop properties,not auto-prop most will be logic and not able to serialize/deserialize
            foreach (var property in targetType.Properties)
            {
                //remove existed auto-prop field
                var autoFieldName = $"<{property.Name}>k__BackingField";
                //auto field only has one field or not
                var relateAutoField = considerFields.Where(x =>
                    x.IsPrivate &&
                    x.Name == autoFieldName &&
                    x.HasCustomAttributes &&
                    x.CustomAttributes.Any(y => y.AttributeType.FullName == _autoPropFieldAttrDef.FullName)
                    ).FirstOrDefault();
                if (relateAutoField != null)
                {
                    // remove them from considerFields
                    considerFields.Remove(relateAutoField);
                }

                //ignore manual mark [IgnoreMember],[IgnoreDataMember]
                if (property.HasCustomAttributes &&
                    property.CustomAttributes.Any(x =>
                        x.AttributeType.FullName == _msgIgnoreDef.FullName ||
                        x.AttributeType.FullName == _msgDataIgnoreDef.FullName))
                    continue;

                if(relateAutoField == null)
                {
                    //not auto-prop + no [IgnoreMember]
                    var attribute = new CustomAttribute(_msgIgnoreConstructor);
                    property.CustomAttributes.Add(attribute);
                    continue;
                }

                // check Accessibility [ignore internal,private]
                // prop won't have Accessibility mark,must check get/set method Accessibility
                // for property,get/set must has at last one public
                var publicGetter = property.GetMethod != null && property.GetMethod.IsPublic ? true : false;
                var publicSetter = property.SetMethod != null && property.SetMethod.IsPublic ? true : false;

                if (!markPrivateField && !(publicGetter || publicSetter))
                {
                    //ignore this prop,but this prop not set [IgnoreMember],add it into that property
                    var attribute = new CustomAttribute(_msgIgnoreConstructor);
                    property.CustomAttributes.Add(attribute);
                    continue;
                }

                considerProperties.Add(property);
            }
            return(considerFields,considerProperties);
        }

        /// <summary>
        /// analysis all [MessagePackObject] class derived relation tree [from base abstract to final class]
        /// </summary>
        /// <param name="module"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        private List<ReferenceTreeNode> GetDerivedRelationprivate(
            ModuleDefinition module,
            IEnumerable<TypeDefinition> types)
        {
            List<ReferenceTreeNode> referenceTree = new List<ReferenceTreeNode>();


            foreach (var typeDef in types)
            {
                //target type exist,ignore
                if (referenceTree.Any(x=>x.node.FullName==typeDef.FullName))
                    continue;

                //struct
                //Must check first beause struct also IsClass
                if (typeDef.IsValueType)
                {
                    var newCurrentType = new ReferenceTreeNode() { node = typeDef };
                    referenceTree.Add(newCurrentType);
                }
                else if(typeDef.IsClass)
                {
                    var newCurrentType = new ReferenceTreeNode() { node = typeDef };
                    referenceTree.Add(newCurrentType);

                    var nextcheck = newCurrentType;

                    while (nextcheck != null && nextcheck.node.FullName != _objectTypeDef.FullName)
                    {
                        var baseType= nextcheck.node.BaseType?.Resolve();
                        if (baseType == null || baseType.FullName == _objectTypeDef.FullName)
                            break;

                        var existType=referenceTree.Where(x=>x.node.FullName==baseType.FullName).FirstOrDefault();

                        if(existType == null)
                        {
                            var newBaseType = new ReferenceTreeNode() { node = baseType };
                            referenceTree.Add(newBaseType);
                            newBaseType.Childs.Add(nextcheck);
                            nextcheck.Parent = newBaseType;
                            nextcheck = newBaseType;
                        }
                        else
                        {
                            existType.Childs.Add(nextcheck);
                            nextcheck.Parent = existType;
                            break;
                        }
                    }
                }
            }
            return referenceTree;
        }



        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "netstandard";
            yield return "mscorlib";
            yield return "System";
            yield return "System.Runtime";
        }

        void InitBasicRequireRef()
        {
            _objectTypeDef = ModuleDefinition.ImportReference(TypeSystem.ObjectDefinition).Resolve();
            _autoPropFieldAttrDef = ModuleDefinition.ImportReference(typeof(CompilerGeneratedAttribute)).Resolve();

            _msgDataIgnoreDef= ModuleDefinition.ImportReference(_msgDataIgnoreAttr).Resolve();

            var msgPackAssembly = new AssemblyNameReference(_msgPackAttrAsName, null);
            try
            {
                _keyMarkDef = ModuleDefinition.AssemblyResolver.Resolve(msgPackAssembly).
                    MainModule.Types.
                    Single(type => type.Name == _msgKeyAttr.Name);

                _msgIgnoreDef = ModuleDefinition.ImportReference(_msgIgnoreAttr).Resolve();

                var msgIgnoreConstructor = _msgIgnoreDef
                                .GetConstructors()
                                .Single(ctor =>
                                    0 == ctor.Parameters.Count);
                _msgIgnoreConstructor = ModuleDefinition.ImportReference(msgIgnoreConstructor);

                var msgKeyConstructor = _keyMarkDef
                                .GetConstructors()
                                .Single(ctor =>
                                    1 == ctor.Parameters.Count &&
                                    TypeSystem.Int32Reference.FullName == ctor.Parameters[0].ParameterType.FullName);
                _msgKeyConstructor = ModuleDefinition.ImportReference(msgKeyConstructor);

            }
            catch (Exception ex)
            {
                WriteError($"Init Basic TypeDefine Failed ({ex.Message})");
            }
        }

        #region GetAssemblyNameSpaceAttributes
        string GetStringFromConfig(string name)
        {
            var attribute = Config?.Attribute(name);
            if (attribute == null)
            {
                return null;
            }
            var value = attribute.Value;
            return value;
        }

        bool GetBoolFromConfig(string name)
        {
            var attribute = Config?.Attribute(name);
            if (attribute == null)
                return false;
            var value = (bool?)attribute;
            if (value == null)
                return false;
            return value.Value;
        }
        #endregion
    }
}
