# csjson

## install
**PM> Install-Package csjson**

## dynamic output
```csharp
dynamic result = JsonTokenizer.Parse(input);
foreach (var item in result.SomeArray)
{
  // stuff
}
```

## deserialize
```csharp
MyType obj = JsonTokenizer.Parse<MyType>(input);
// includes DateTime parsing
```

## serialization
// not included (yet)