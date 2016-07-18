using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MeterMateUwp
{
    public sealed partial class Thermometer : UserControl
    {
        public static readonly DependencyProperty MinimumTemperatureProperty = DependencyProperty.Register("MinimumTemperature", typeof(double), typeof(Thermometer), null);
        public static readonly DependencyProperty MaximumTemperatureProperty = DependencyProperty.Register("MaximumTemperature", typeof(double), typeof(Thermometer), null);
        public static readonly DependencyProperty TemperatureProperty = DependencyProperty.Register("Temperature", typeof(double), typeof(Thermometer), new PropertyMetadata("", new PropertyChangedCallback(OnTemperatureChanged)));

        public Thermometer()
        {
            this.InitializeComponent();
        }

        public double MinimumTemperature
        {
            get; set;
        }

        public double MaximumTemperature
        {
            get; set;
        }

        public double Temperature
        {
            get
            {
                return (double)GetValue(TemperatureProperty);
            }

            set
            {
                SetValue(TemperatureProperty, value);

                myCanvas.Invalidate();
            }
        }

        private static void OnTemperatureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            int n = 0;
        }

        private void OnTemperatureChanged(DependencyPropertyChangedEventArgs e)
        {
            int n = 1;
        }

        private void CanvasControl_Draw(CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            double actualWidth = sender.ActualWidth;
            double actualHeight = sender.ActualHeight;

            float xCentreBottomCircle = (float)(actualWidth / 2);
            float yCenterBottomCircle = (float)(actualHeight - (actualWidth / 2));
            float outerRadiusBottomCircle = (float)(actualWidth / 2);

            float outerRadiusTopCircle = (float)(outerRadiusBottomCircle / 2);
            float xCenterTopCircle = (float)(actualWidth / 2);
            float yCentreTopCircle = (float)(outerRadiusTopCircle);

            float outerRectangleWidth = (float)(actualWidth / 2);
            float outerRectangleHeight = (float)(actualHeight - outerRadiusBottomCircle - outerRadiusTopCircle);
            float outerRectangleTop = outerRadiusTopCircle;
            float outerRectangleLeft = (float)(actualWidth - outerRectangleWidth) / 2;

            // Black Background
            args.DrawingSession.FillCircle(xCenterTopCircle, yCentreTopCircle, outerRadiusTopCircle, Colors.Black);
            args.DrawingSession.FillRectangle(outerRectangleLeft, outerRectangleTop, outerRectangleWidth, outerRectangleHeight, Colors.Black);
            args.DrawingSession.FillCircle(xCentreBottomCircle, yCenterBottomCircle, outerRadiusBottomCircle, Colors.Black);

            float innerRadiusTopCircle = outerRadiusTopCircle - 5f;

            float innerRadiusBottomCircle = outerRadiusBottomCircle - 5f;

            float innerRectangleWidth = outerRectangleWidth - 10f;
            float innerRectangleHeight = outerRectangleHeight;
            float innerRectangleTop = outerRadiusTopCircle;
            float innerRectangleLeft = (float)(actualWidth - innerRectangleWidth) / 2;

            // Inner parts
            args.DrawingSession.FillCircle(xCenterTopCircle, yCentreTopCircle, innerRadiusTopCircle, Colors.White);
            args.DrawingSession.FillRectangle(innerRectangleLeft, innerRectangleTop, innerRectangleWidth, innerRectangleHeight, Colors.White);
            args.DrawingSession.FillCircle(xCentreBottomCircle, yCenterBottomCircle, innerRadiusBottomCircle, Colors.Red);

            double usedValue = Temperature;

            if (usedValue > MaximumTemperature)
            {
                usedValue = MaximumTemperature;
            }

            if (usedValue < MinimumTemperature)
            {
                usedValue = MinimumTemperature;
            }

            float valueRectangleBottom = (float)(actualHeight - (2 * outerRadiusBottomCircle));

            float maximumValueLength = valueRectangleBottom - outerRadiusTopCircle;

            // Scale
            args.DrawingSession.FillRectangle(innerRectangleLeft, valueRectangleBottom, innerRectangleWidth, outerRadiusBottomCircle, Colors.Red);

            float valueLength = (float)(maximumValueLength * (Temperature / (MaximumTemperature - MinimumTemperature)));

            args.DrawingSession.FillRectangle(innerRectangleLeft, valueRectangleBottom - valueLength, innerRectangleWidth, valueLength, Colors.Red);

            // Display as text
            string text = string.Format("{0:+#0.0;-#0.0;0.0} °C", Temperature);

            FontWeight weight = new FontWeight();
            weight.Weight = 900;

            CanvasTextFormat format = new CanvasTextFormat { FontSize = 20f, FontWeight = weight, WordWrapping = CanvasWordWrapping.NoWrap, };

            CanvasTextLayout textLayout = new CanvasTextLayout(args.DrawingSession, text, format, 0.0f, 0.0f);

            float xLoc = xCentreBottomCircle - ((float)textLayout.DrawBounds.Width / 2);
            float yLoc = yCenterBottomCircle - ((float)textLayout.DrawBounds.Height);

            args.DrawingSession.DrawTextLayout(textLayout, xLoc, yLoc, Colors.White);
        }
    }
}
