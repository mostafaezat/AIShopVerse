using System.Text.Json;
using AIShopVerse.EndUser.Server;
using Domain.Entities.PaymentEntities;
using Xunit;

namespace Application.Tests.Serialization
{
    public class EnumStringOrNumberConverterTests
    {
        private sealed class Sample
        {
            public PaymentMethod PaymentMethod { get; set; }
        }

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new EnumStringOrNumberConverterFactory());
            return options;
        }

        [Theory]
        [InlineData("\"CashOnDelivery\"")]
        [InlineData("\"cashondelivery\"")]
        [InlineData("0")]
        [InlineData("1")]
        public void Deserializes_string_and_number_enum_values(string jsonValue)
        {
            var options = CreateOptions();
            var sample = JsonSerializer.Deserialize<Sample>($"{{\"paymentMethod\":{jsonValue}}}", options);

            Assert.NotNull(sample);
        }

        [Fact]
        public void String_input_binds_to_matching_member()
        {
            var options = CreateOptions();
            var sample = JsonSerializer.Deserialize<Sample>("{\"paymentMethod\":\"CashOnDelivery\"}", options);

            Assert.Equal(PaymentMethod.CashOnDelivery, sample!.PaymentMethod);
        }

        [Fact]
        public void Number_input_binds_to_matching_member()
        {
            var options = CreateOptions();
            var sample = JsonSerializer.Deserialize<Sample>("{\"paymentMethod\":1}", options);

            Assert.Equal(PaymentMethod.CreditCard, sample!.PaymentMethod);
        }

        [Fact]
        public void Serialization_keeps_numeric_output()
        {
            var options = CreateOptions();
            var json = JsonSerializer.Serialize(new Sample { PaymentMethod = PaymentMethod.CashOnDelivery }, options);

            Assert.Equal("{\"PaymentMethod\":0}", json);
        }

        [Fact]
        public void Unknown_string_throws_JsonException()
        {
            var options = CreateOptions();

            Assert.Throws<JsonException>(() =>
                JsonSerializer.Deserialize<Sample>("{\"paymentMethod\":\"Bitcoin\"}", options));
        }
    }
}