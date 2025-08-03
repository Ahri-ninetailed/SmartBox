using System.Text.Json;
using Xunit.Abstractions;

namespace Infrastructure.Tests
{
    //https://stackoverflow.com/questions/30574322/memberdata-tests-show-up-as-one-test-instead-of-many
    public class MemberDataSerializer<T> : IXunitSerializable
    {
        public T Object { get; private set; }

        public MemberDataSerializer()
        {
            Object = default!;
        }

        public MemberDataSerializer(T objectToSerialize)
        {
            Object = objectToSerialize;
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Object = JsonSerializer.Deserialize<T>(info.GetValue<string>("objValue"))!;
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            var json = JsonSerializer.Serialize(Object);
            info.AddValue("objValue", json);
        }
    }
}
