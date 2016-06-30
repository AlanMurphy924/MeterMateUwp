using System;
using MeterMateUwp;
using Xunit;

namespace MeterMateUwpTests
{
    public class TemperatureTests
    {
        [Fact]
        public void WaterFreezingPointInCelsius()
        {
            Temperature t = Temperature.CreateFromCelsius(0.0);

            Assert.Equal(0.0, t.Celsius, 5);
            Assert.Equal(32.0, t.Fahrenheit, 5);
            Assert.Equal(273.15, t.Kelvins, 5);
        }

        [Fact]
        public void WaterFreezingPointInKelvins()
        {
            Temperature t = Temperature.CreateFromKelvins(273.15);

            Assert.Equal(0.0, t.Celsius, 5);
            Assert.Equal(32.0, t.Fahrenheit, 5);
            Assert.Equal(273.15, t.Kelvins, 5);
        }

        [Fact]
        public void WaterFreezingPointInFahrenheit()
        {
            Temperature t = Temperature.CreateFromFahrenheit(32.0);

            Assert.Equal(0.0, t.Celsius, 5);
            Assert.Equal(32.0, t.Fahrenheit, 5);
            Assert.Equal(273.15, t.Kelvins, 5);
        }

        [Fact]
        public void WaterBoilingPointInCelsius()
        {
            Temperature t = Temperature.CreateFromCelsius(100.0);

            Assert.Equal(100.0, t.Celsius, 5);
            Assert.Equal(212.0, t.Fahrenheit, 5);
            Assert.Equal(373.15, t.Kelvins, 5);
        }

        [Fact]
        public void WaterBoilingPointInFahrenheit()
        {
            Temperature t = Temperature.CreateFromFahrenheit(212.0);

            Assert.Equal(100.0, t.Celsius, 5);
            Assert.Equal(212.0, t.Fahrenheit, 5);
            Assert.Equal(373.15, t.Kelvins, 5);
        }

        [Fact]
        public void WaterBoilingPointInKelvins()
        {
            Temperature t = Temperature.CreateFromKelvins(373.15);

            Assert.Equal(100.0, t.Celsius, 5);
            Assert.Equal(212.0, t.Fahrenheit, 5);
            Assert.Equal(373.15, t.Kelvins, 5);
        }

        [Fact]
        public void TemperatureInKelvinsTooLow_ThrowsException()
        {
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => Temperature.CreateFromKelvins(-10.0));

            Assert.Equal("temperature", ex.ParamName);
        }

        [Fact]
        public void TemperatureInCelsiusTooLow_ThrowsException()
        {
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => Temperature.CreateFromCelsius(-1000.0));

            Assert.Equal("temperature", ex.ParamName);
        }

        [Fact]
        public void TemperatureInFahrenheitTooLow_ThrowsException()
        {
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => Temperature.CreateFromFahrenheit(-1000.0));

            Assert.Equal("temperature", ex.ParamName);
        }
    }
}
