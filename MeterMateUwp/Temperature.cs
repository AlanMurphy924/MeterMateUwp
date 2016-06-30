using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeterMateUwp
{
    public class Temperature
    {
        // Private member to store the temperature in Kelvins
        private double temperature;

        public double Kelvins
        {
            get
            {
                return this.temperature;
            }
        }

        public double Celsius
        {
            get
            {
                return KelvinToCelsius(this.temperature);
            }
        }

        public double Fahrenheit
        {
            get
            {
                return KelvinToFahrenheit(this.temperature);
            }
        }

        private Temperature()
        {

        }

        public static Temperature CreateFromKelvins(double temperature)
        {
            if (temperature < 0.0)
            {
                throw new ArgumentOutOfRangeException("temperature", "Less than absolute zero");
            }

            Temperature t = new Temperature();

            t.temperature = temperature;

            return t;
        }

        public static Temperature CreateFromCelsius(double temperature)
        {
            if (temperature < -273.15)
            {
                throw new ArgumentOutOfRangeException("temperature", "Less than -273.15 C");
            }

            Temperature t = new Temperature();

            t.temperature = CelsiusToKelvin(temperature);

            return t;
        }

        public static Temperature CreateFromFahrenheit(double temperature)
        {
            if (temperature < -459.67)
            {
                throw new ArgumentOutOfRangeException("temperature", "Less than -459.67 F");
            }

            Temperature t = new Temperature();

            t.temperature = FahrenheitToKelvin(temperature);

            return t;
        }

        private static double FahrenheitToCelsius(double v)
        {
            return (v - 32) / 1.8;
        }

        private static double CelsiusToFahrenheit(double v)
        {
            return (v * 1.8) + 32.0;
        }

        private static double CelsiusToKelvin(double v)
        {
            return v + 273.15;
        }

        private static double KelvinToCelsius(double v)
        {
            return v - 273.15;
        }

        private static double FahrenheitToKelvin(double v)
        {
            return CelsiusToKelvin(FahrenheitToCelsius(v));
        }

        private static double KelvinToFahrenheit(double v)
        {
            return CelsiusToFahrenheit(KelvinToCelsius(v));
        }
    }
}
