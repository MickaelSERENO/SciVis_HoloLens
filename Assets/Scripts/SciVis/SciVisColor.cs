using UnityEngine;

namespace Sereno.SciVis
{
    public class SciVisColor
    {
        //The color needed
        private static readonly Color    coldColorRGB  = new Color(59.0f / 255.0f, 76.0f / 255.0f, 192.0f / 255.0f, 1.0f);
        private static readonly Color    warmColorRGB  = new Color(180.0f / 255.0f, 4.0f / 255.0f, 38.0f / 255.0f, 1.0f);

        private static readonly LABColor coldColorLAB  = new LABColor(coldColorRGB);
        private static readonly LABColor warmColorLAB  = new LABColor(warmColorRGB);
        private static readonly LABColor whiteColorLAB = new LABColor(XYZColor.Reference);

        private static readonly LUVColor coldColorLUV  = new LUVColor(coldColorRGB);
        private static readonly LUVColor warmColorLUV  = new LUVColor(warmColorRGB);
        private static readonly LUVColor whiteColorLUV = new LUVColor(XYZColor.Reference);

        private static readonly MSHColor coldColorMSH  = new MSHColor(coldColorRGB);
        private static readonly MSHColor warmColorMSH  = new MSHColor(warmColorRGB);

        public static Color GenColor(ColorMode mode, float t)
        {
            switch(mode)
            {
                case ColorMode.RAINBOW:
                    return new HSVColor(260.0f*t, 1.0f, 1.0f).ToRGB();
                case ColorMode.GRAYSCALE:
                    return new Color(t, t, t);
                case ColorMode.WARM_COLD_CIELAB:
                {
                    if(t < 0.5f)
                        return (coldColorLAB*(1.0f-2.0f*t) + whiteColorLAB*(2.0f*t)).ToXYZ().ToRGB();
                    return (whiteColorLAB*(2.0f-2.0f*t) + warmColorLAB*(2.0f*t-1.0f)).ToXYZ().ToRGB(); 
                }
                case ColorMode.WARM_COLD_CIELUV:
                {
                    if(t < 0.5f)
                        return (coldColorLUV*(1.0f-2.0f*t) + whiteColorLUV*(2.0f*t)).ToXYZ().ToRGB();
                    return (whiteColorLUV*(2.0f-2.0f-t) + warmColorLUV*(2.0f*t-1.0f)).ToXYZ().ToRGB();
                }
                case ColorMode.WARM_COLD_MSH:
                {
                    MSHColor col = new MSHColor(0.0f, 0.0f, 0.0f);
                    col.FromColorInterpolation(coldColorMSH, warmColorMSH, t);
                    return col.ToXYZColor().ToRGB();
                }
            }

            return new Color(0, 0, 0, 0);
        }
    }
}