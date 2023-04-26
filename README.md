# AutoPolymorphicMessagePack
Auto Union Scanner For [MessagePack-CSharp](https://github.com/neuecc/MessagePack-CSharp)

Let you use more easy way to union messagepack object

## How To Use
Mark which abstract class or interface you want to union for (e.g.these class are in `Project1` project range)

```C#
    //mark abstract class
    [UnionAbsOrInterface]
    public abstract class CBase1
    {
    
    }
    //mark interface
    [UnionAbsOrInterface]
    public interface IBase1
    {

    }
    // generic interface or abstract also support
    [UnionAbsOrInterface]
    public interface IBase4<T>
    {

    }
```

AutoPolymorphicMessagePack use Fody Plugin to weave in automatically at compile time,I haven't publish nuget package yet,
so you need add [PolymorphicMessagePack.Fody](https://github.com/PatchouliTC/PolymorphicMessagePack/tree/master/PolymorphicMessagePack.Fody) into your project

Then set your `Project1` follow these steps:

  1. import [Fody Nuget Package](https://www.nuget.org/packages/Fody) into `Project1` 
  
      **Don't worry about this package,it won't be your project reference when you compile `Project1`**
    
  2. add an entry to the `Project1.csproj` file:
  
```xml
  <ItemGroup>
    <WeaverFiles Include="$(SolutionDir)PolymorphicMessagePack.Fody\bin\$(Configuration)\netstandard2.0\PolymorphicMessagePack.Fody.dll" 
     WeaverClassNames="MsgPackPolyWeaver" />
  </ItemGroup>
```

  3. Change the [solution build order](https://docs.microsoft.com/en-au/visualstudio/ide/how-to-create-and-remove-project-dependencies) so the 'Project1' project is built before the projects consuming it.
  4. Compile `Project1`,Fody will generate `FodyWeavers.xml` into project when not found that file
  
  then write config into `FodyWeavers.xml`:
  
```xml
<Weavers xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="FodyWeavers.xsd">
  <MsgPackPolyWeaver NameSpace='Project1NameSpaceWhichContainMsgPackMarkedClass'/>
</Weavers>
```
  5. prepare your msgpack marked object into `Project1`:
  
```C#
    //if non generic type,you need do nothing,Fody will add mark and unique id attr into it
    [MessagePackObject]
    public class Class1 : CBase1
    {
        [Key(0)]
        public long CT1 { get; set; }
    }
    
    // if generic object,all actual generic types to be used must be declared
    [GenericUnion(typeof(int))]
    [GenericUnion(typeof(string))]
    [MessagePackObject]
    public class Class2<T> : IBase4<T>
    {
        [Key(0)]
        public long CT2 { get; set; }
    }
    
    //if you want to fixed union id manually,mark it,fody will ignore this type and avoid use fixed id to mark other types
    [RequireUnion(1)]
    [MessagePackObject]
    public class Class3 : CBase1
    {
        [Key(0)]
        public long CT1 { get; set; }
    }
    
    //also work for generic type,but you must make sure type in [RequireUnionGeneric] is current generic type or fody will give complie error
    [RequireUnionGeneric(10,typeof(Class2<int>))]
    [GenericUnion(typeof(string))]
    [MessagePackObject]
    public class Class2<T> : IBase4<T>
    {
        [Key(0)]
        public long CT2 { get; set; }
    }
```
  6. using and inject `Project1` assembly into `PolymorphicMessagePackSettings` and use it
  
```C#
  var polySettings = new PolymorphicMessagePackSettings();
  polySettings.InjectUnionRequireFromAssembly(typeof(Project1NameSpaceWhichContainMsgPackMarkedClass).Assembly);
  
  //serialize fact instance
  var s1 = MessagePackSerializer.Serialize(new Class1 { CT1 = 1 }, _options);
  
  //deserialize it to abstract
  var ds1 = MessagePackSerializer.Deserialize<CBase1>(s1, _options);
  
  //serialize marked generic also allowed
   var s2 = MessagePackSerializer.Serialize(new Class2<int> { CT2 = 2 }, _options);
  
  //deserialize it to generic interface
  var ds2 = MessagePackSerializer.Deserialize<IBase4<int>>(s2, _options);
```

_You can see more in [PolyMsgPack.Test](https://github.com/PatchouliTC/PolymorphicMessagePack/tree/master/PolymorphicMessagePack.Fody),[MsgPackDefineForInject](https://github.com/PatchouliTC/PolymorphicMessagePack/tree/master/MsgPackDefineForInject) and use `ILSpy` to see how it works_

## Note

  1. This tool will automatic **give each derivedType unique id** to distinguish which type is when deserialize

      Also you can use `[RequireUnion(1)]`(For non generic) or `[RequireUnionGeneric(2,typeof(Class2<int>))]`(For generic) to manual fixed id
      
      **it can Fixed target type match id and won't change id in diff version**
  2. Fody plugin also will Check each fixed id,If the same Id is pointed to a different type, **it will prevent compilation and indicate the specific conflict type**
  
  3. If you want to use _Fody.Test_ to see _PolymorphicMessagePack.Fody_ works,make sure change `MsgPackDefineForInject` project property:
```xml
    <DisableFody>true</DisableFody>
```
