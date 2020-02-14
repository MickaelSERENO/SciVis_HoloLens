using System;
using Unity.Burst;
using UnityEngine;

//For this file, 

namespace Sereno
{
    /// <summary>
    /// Hue Saturation Value color model https://en.wikipedia.org/wiki/HSL_and_HSV
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct HSVColor
    {
        /// <summary>
        /// HSV Constructor
        /// </summary>
        /// <param name="h">The Hue component</param>
        /// <param name="s">The Saturation component</param>
        /// <param name="v">The Value component</param>
        /// <param name="a">The transparency component</param>
        public HSVColor(float h, float s, float v, float a=1.0f)
        {
            H = h;
            S = s;
            V = v;
            A = a;
        }

        public HSVColor(Color color)
        {
            H = S = V = A = 0;
            SetFromRGB(color);
        }

        /// <summary>
        /// Set the HSV value from RGB color 
        /// </summary>
        /// <param name="color">The color to convert</param>
        public void SetFromRGB(Color color)
        {
            float max = (float)Math.Max(Math.Max(color.r, color.g), color.b);
            float min = (float)Math.Min(Math.Min(color.r, color.g), color.b);
            float c   = max-min;

            //Compute the Hue
            if(c == 0)
                H = 0;
            else if(max == color.r)
                H = (color.g - color.b)/c;
            else if(max == color.g)
                H = (color.b - color.r)/c;
            else if(max == color.b)
                H = (color.r - color.g)/c;
            H *= 60.0f;

            //Compute the Saturation
            if(max == 0)
                S = 0;
            else
                S = c/max;

            //Compute the Value
            V = max;
        }

        /// <summary>
        /// Convert the HSV color to RGB color
        /// </summary>
        /// <returns>The color in RGB colorspace</returns>
        public Color ToRGB()
        {
            float c = V*S;
            float h = H/60.0f;
            float x = c*(1.0f-Math.Abs(h%2.0f - 1.0f));
            float m = V - c;
            switch((int)h)
            {
                case 0:
                    return new Color(c+m, x+m, m, A);
                case 1:
                    return new Color(x+m, c+m, m, A);
                case 2:
                    return new Color(m, c+m, x+m, A);
                case 3:
                    return new Color(m, x+m, c+m, A);
                case 4:
                    return new Color(x+m, m, c+m, A);
                default:
                    return new Color(c+m, m, x+m, A);
            }
        }

        /// <summary>
        /// The Hue component defined in degree
        /// </summary>
        public float H { get; set;}

        /// <summary>
        /// The Saturation component
        /// </summary>
        public float S { get; set;}

        /// <summary>
        /// The Value component
        /// </summary>
        public float V { get; set;}

        /// <summary>
        /// The Transparency component
        /// </summary>
        public float A { get; set;}
    }

    /// <summary>
    /// the XYZ Color space : https://en.wikipedia.org/wiki/_1931_color_space#Meaning_of_X,_Y_and_Z
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct XYZColor
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x2">X component</param>
        /// <param name="y2">Y component</param>
        /// <param name="z2">Z component</param>
        /// <param name="a2">transparency component</param>
        public XYZColor(float x2, float y2, float z2, float a2=1.0f)
        {
            X = x2;
            Y = y2;
            Z = z2;
            A = a2;
        }

        /// <summary>
        /// Constructor. Convert RGB color space into XYZ Colorspace
        /// </summary>
        /// <param name="color">color the RGB color</param>
        public XYZColor(Color color)
        {
            X = (float)(color.r*0.4124 + color.g*0.3576 + color.b*0.1805);
            Y = (float)(color.r*0.2126 + color.g*0.7152 + color.b*0.0722);
            Z = (float)(color.r*0.0193 + color.g*0.1192 + color.b*0.9505);
            A = color.a;
        }

        public Color ToRGB()
        {
            Color c = new Color(3.2405f*X - 1.5371f*Y - 0.4985f*Z,
                               -0.9692f*X + 1.8760f*Y + 0.0415f*Z,
                                0.0556f*X - 0.2040f*Y + 1.0572f*Z,
                                A);
            if (c.r > 1.0f)
                c.r = 1.0f;
            if (c.b > 1.0f)
                c.b = 1.0f;
            if (c.g > 1.0f)
                c.g = 1.0f;
            return c;
        }

        /// <summary>
        /// The X component
        /// </summary>
        public float X{get; set;}

        /// <summary>
        /// The Y component
        /// </summary>
        public float Y{get; set;}

        /// <summary>
        /// The Z component
        /// </summary>
        public float Z{get; set;}

        /// <summary>
        /// The transparency component
        /// </summary>
        public float A{get; set;}

        /// <summary>
        /// the XYZ of reference (white), also called XYZ_s
        /// </summary>
        static public XYZColor Reference = new XYZColor(0.9505f, 1.0f, 1.0890f, 1.0f);
    }

    /// <summary>
    /// the LAB colorspace https://en.wikipedia.org/wiki/LAB_color_space
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct LABColor
    {
        /// <summary>
        /// Constructor copy
        /// </summary>
        /// <param name="color">The color to copy</param>
        public LABColor(LABColor color)
        {
            L = color.L;
            A = color.A;
            B = color.B;
            Transparency = color.Transparency;
        }

        /// <summary>
        /// Constructor, determines L, A and B values from RGB colorspace
        /// </summary>
        /// <param name="color">The RGB color to transform</param>
        public LABColor(Color color)
        {
            L = A = B = Transparency = 0;
            SetFromRGB(color);
        }

        /// <summary>
        /// Constructor, determines L, A and B values from XYZ colorspace
        /// </summary>
        /// <param name="xyz">The XYZ color to transform</param>
        public LABColor(XYZColor xyz)
        {
            L = A = B = Transparency = 0;
            SetFromXYZ(xyz);
        }

        /// <summary>
        /// Constructor, constructs this object with default values
        /// </summary>
        /// <param name="l2">the L component</param>
        /// <param name="a2">the A component</param>
        /// <param name="b2">the B component</param>
        /// <param name="trans">the transparency component</param>
        public LABColor(float l2, float a2, float b2, float trans=1.0f)
        {
            L = l2;
            A = a2;
            B = b2;
            Transparency = trans;
        }

        /// <summary>
        /// Set the L, A and B values from RGB colorspace value
        /// </summary>
        /// <param name="color">the color to transform</param>
        public void SetFromRGB(Color color)
        {
            XYZColor xyz = new XYZColor(color);
            SetFromXYZ(xyz);
        }

        /// <summary>
        /// Set the L, A and B values from XYZ colorspace value
        /// </summary>
        /// <param name="xyz">color the color to transform</param>
        public void SetFromXYZ(XYZColor xyz)
        {
            float fX = F(xyz.X/XYZColor.Reference.X);
            float fY = F(xyz.Y/XYZColor.Reference.Y);
            float fZ = F(xyz.Z/XYZColor.Reference.Z);

            L = 116*fY - 16.0f;
            A = 500*(fX - fY);
            B = 200*(fY - fZ);

            Transparency = xyz.A;
        }

        /// <summary>
        /// Transform the LAB colorspace to XYZ colorspace
        /// </summary>
        /// <returns>the XYZ colorspace value</returns>
        public XYZColor ToXYZ()
        {
            return new XYZColor(XYZColor.Reference.X * InvF((float)((L+16.0)/116.0 + A/500.0)),
                                XYZColor.Reference.Y * InvF((float)((L+16.0)/116.0)),
                                XYZColor.Reference.Z * InvF((float)((L+16.0)/116.0 - B/200.0)),
                                Transparency);
        }

        /// <summary>
        /// Make a linear interpolation between two LAB Color
        /// </summary>
        /// <param name="c1">the first color (t=0)</param>
        /// <param name="c2">the second color (t=1)</param>
        /// <param name="t">the progression factor (between 0 and 1)</param>
        /// <returns>The interpolated color</returns>
        public static LABColor Lerp(LABColor c1, LABColor c2, float t)
        {
            return c1*(1.0f-t) + c2*t;
        }

        /// <summary>
        /// Multiply the components of a color by a constant
        /// </summary>
        /// <param name="c1">The color</param>
        /// <param name="t">The constant</param>
        /// <returns>c1*t</returns>
        public static LABColor operator *(LABColor c1, float t)
        {
            return new LABColor(c1.L*t, c1.A*t, c1.B*t, c1.Transparency*t);
        }

        /// <summary>
        /// Multiply the components of a color by a constant
        /// </summary>
        /// <param name="t">The constant</param>
        /// <param name="c1">The color</param>
        /// <returns>c1*t</returns>
        public static LABColor operator *(float t, LABColor c1)
        {
            return c1*t;
        }

        /// <summary>
        /// Apply - operation on colors components
        /// </summary>
        /// <param name="c1">The left operation color</param>
        /// <param name="c2">The right operation color</param>
        /// <returns>c1-c2</returns>
        public static LABColor operator -(LABColor c1, LABColor c2)
        {
            return new LABColor(c1.L-c2.L, c1.A-c2.A, c1.B-c2.B, c1.Transparency-c2.Transparency);
        }

        /// <summary>
        /// Apply + operation on colors components
        /// </summary>
        /// <param name="c1">The left operation color</param>
        /// <param name="c2">The right operation color</param>
        /// <returns>c1+c2</returns>
        public static LABColor operator +(LABColor c1, LABColor c2)
        {
            return new LABColor(c1.L+c2.L, c1.A+c2.A, c1.B+c2.B, c1.Transparency+c2.Transparency);
        }

        /// <summary>
        /// a private function which helps determining the three component value.
        /// 7.787*v+16.0/116.0 otherwise. theta = 6.0/29.0 -> theta^3 = 0.008856
        /// </summary>
        /// <param name="v">the value to determine</param>
        /// <returns>v^(1.0/3.0) if v > 0.008856</returns>
        private float F(float v) => (float)(v > 0.008856 ? Math.Pow(v, 1.0/3.0) : 7.787*v + 16.0f/116.0f);

        /// <summary>
        /// the inverse function which helps determining the three component value.
        /// 0.128418*(v-4.0/29.0) otherwise, thata = 6.0/29.0 -> 3*theta^2 = 0.128418
        /// </summary>
        /// <param name="v">the value to determine</param>
        /// <returns>v^/3.0 if v > 6.0/29.0</returns>
        private float InvF(float v) => (float)(v > 6.0f/29.0f ? v*v*v : 0.128418f*(v - 4.0f/29.0f));

        /// <summary>
        /// The L component
        /// </summary>
        public float L;

        /// <summary>
        /// The A component
        /// </summary>
        public float A;

        /// <summary>
        /// The B component
        /// </summary>
        public float B;


        /// <summary>
        /// The transparency
        /// </summary>
        public float Transparency;
    }

    /// <summary>
    /// LUV colorspace https://en.wikipedia.org/wiki/CIELUV
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    public struct LUVColor
    {
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="color">The color to copy</param>
        public LUVColor(LUVColor color)
        {
            L = color.L;
            U = color.U;
            V = color.V;
            A = color.A;
        }

        /// <summary>
        /// Basic constructor, initialize the object with default values
        /// </summary>
        /// <param name="l2">the L component</param>
        /// <param name="u2">the U component</param>
        /// <param name="v2">the V component</param>
        /// <param name="trans">the transparency</param>
        public LUVColor(float l2, float u2, float v2, float trans=1.0f)
        {
            L = l2;
            U = u2;
            V = v2;
            A = trans;
        }

        /// <summary>
        /// Constructor, determines L, U and V values from RGB colorspace
        /// </summary>
        /// <param name="color">The RGB color value</param>
        public LUVColor(Color color)
        {
            L = U = V = A = 0;
            SetFromRGB(color);
        }

        /// <summary>
        /// Constructor, determines L, U and V values from XYZ colorspace
        /// </summary>
        /// <param name="xyz">The XYZ color value</param>
        public LUVColor(XYZColor xyz)
        {
            L = U = V = A = 0;
            SetFromXYZ(xyz);
        }

        /// <summary>
        /// Set the L, U and V values from RGB colorspace value
        /// </summary>
        /// <param name="color">the color to transform</param>
        public void SetFromRGB(Color color)
        {
            XYZColor xyz = new XYZColor(color);
            SetFromXYZ(xyz);
        }

        /// <summary>
        /// Set the L, U and V values from XYZ colorspace value
        /// </summary>
        /// <param name="xyz">the color to transform</param>
        public void SetFromXYZ(XYZColor xyz)
        {
            float un = 4*XYZColor.Reference.X/(XYZColor.Reference.X+15*XYZColor.Reference.Y+3*XYZColor.Reference.Z);
            float vn = 9*XYZColor.Reference.Y/(XYZColor.Reference.X+15*XYZColor.Reference.Y+3*XYZColor.Reference.Z);

            float y = xyz.Y/XYZColor.Reference.Y; 
            if(y < 0.008856f)      //(6/29)**3 =   0.008856
                L = 903.296296f*y; //(29/3)**3 = 903.296296
            else
                L = 116.0f*(float)(Math.Pow(y, 1.0f/3.0f)) - 16.0f;

            U = 13.0f*L * (4.0f*xyz.X/(xyz.X + 15.0f*xyz.Y + 3.0f*xyz.Z) - un);
            V = 13.0f*L * (9.0f*xyz.Y/(xyz.X + 15.0f*xyz.Y + 3.0f*xyz.Z) - vn);

            A = xyz.A;
        }

        /*\brief Transform the LUV colorspace to XYZ colorspace
         * \return the XYZ colorspace value*/
        public XYZColor ToXYZ()
        {
            float un = 4*XYZColor.Reference.X/(XYZColor.Reference.X+15*XYZColor.Reference.Y+3*XYZColor.Reference.Z);
            float vn = 9*XYZColor.Reference.Y/(XYZColor.Reference.X+15*XYZColor.Reference.Y+3*XYZColor.Reference.Z);

            float uprime = U/(13.0f*L) + un;
            float vprime = V/(13.0f*L) + vn;

            float z = 0.0f;
            float y = 0.0f;
            float x = 0.0f;

            if(L <= 8.0)
                y = XYZColor.Reference.Y*L*(0.001107056f); //0.001107056 = (3.0/29.0)**3
            else
            {
                float lprime = (L+16.0f)/116.0f;
                y = XYZColor.Reference.Y*lprime*lprime*lprime;
            }
            x = y*9*uprime/(4*vprime);
            z = y*(12 - 3*uprime - 20*vprime)/(4*vprime);

            return new XYZColor(x, y, z, A);
        }

        /// <summary>
        /// Make a linear interpolation between two LAB Color
        /// </summary>
        /// <param name="c1">the first color (t=0)</param>
        /// <param name="c2">the second color (t=1)</param>
        /// <param name="t">the progression factor (between 0 and 1)</param>
        /// <returns>The interpolated color</returns>
        public static LUVColor Lerp(LUVColor c1, LUVColor c2, float t)
        {
            return new LUVColor(c1*(1.0f-t) + c2*t);
        }

        /// <summary>
        /// Multiply the components of a color by a constant
        /// </summary>
        /// <param name="c1">The color</param>
        /// <param name="t">The constant</param>
        /// <returns>c1*t</returns>
        public static LUVColor operator*(LUVColor c1, float t)
        {
            return new LUVColor(c1.L*t, c1.U*t, c1.V*t, c1.A*t);
        }

        /// <summary>
        /// Multiply the components of a color by a constant
        /// </summary>
        /// <param name="t">The constant</param>
        /// <param name="c1">The color</param>
        /// <returns>c1*t</returns>
        public static LUVColor operator*(float t, LUVColor c1)
        {
            return c1 * t;
        }

        /// <summary>
        /// Apply - operation on colors components
        /// </summary>
        /// <param name="c1">The left operation color</param>
        /// <param name="c2">The right operation color</param>
        /// <returns>c1-c2</returns>
        public static LUVColor operator-(LUVColor c1, LUVColor c2)
        {
            return new LUVColor(c1.L-c2.L, c1.U-c2.U, c1.V-c2.V, c1.A-c2.A);
        }

        /// <summary>
        /// Apply + operation on colors components
        /// </summary>
        /// <param name="c1">The left operation color</param>
        /// <param name="c2">The right operation color</param>
        /// <returns>c1+c2</returns>
        public static LUVColor operator+(LUVColor c1, LUVColor c2)
        {
            return new LUVColor(c1.L+c2.L, c1.U+c2.U, c1.V+c2.V, c1.A+c2.A);
        }

        /// <summary>
        /// The L component
        /// </summary>
        public float L{get; set;}

        /// <summary>
        /// The U component
        /// </summary>
        public float U{get; set;}

        /// <summary>
        /// The V component
        /// </summary>
        public float V{get; set;}

        /// <summary>
        /// The transparency component
        /// </summary>
        public float A{get; set;}
    }

    /* \brief The MSH Colorspace (see Diverging Color Maps for Scientific Visualization)*/
    [BurstCompile(CompileSynchronously = true)]
    public struct MSHColor
    {
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copy">The color to copy</param>
        public MSHColor(MSHColor copy)
        {
            M = copy.M;
            S = copy.S;
            H = copy.H;
            A = copy.A;
        }

        /// <summary>
        /// MSH constructor
        /// </summary>
        /// <param name="m2">M value</param>
        /// <param name="s2">S value</param>
        /// <param name="h2">H value</param>
        /// <param name="trans">Transparency</param>
        public MSHColor(float m2, float s2, float h2, float trans = 1.0f)
        {
            M = m2;
            S = s2;
            H = h2;
            A = trans;
        }

        /// <summary>
        /// MSH Constructor initialized from RGB
        /// </summary>
        /// <param name="color">The RGB color to convert</param>
        public MSHColor(Color color)
        {
            M = S = H = A = 0;
            FromRGB(color);
        }

        /// <summary>
        /// MSH Constructor initialized from XYZ
        /// </summary>
        /// <param name="color">The XYZ color to convert</param>
        public MSHColor(XYZColor color)
        {
            M = S = H = A = 0;
            FromLABColor(new LABColor(color));
        }

        /// <summary>
        /// MSH Constructor initialized from CIELAB
        /// </summary>
        /// <param name="color">the CIELAB color to convert</param>
        public MSHColor(LABColor color)
        {
            M = S = H = A = 0;
            FromLABColor(color);
        }

        /// <summary>
        /// Make an interpolation from c1 to c2 
        /// </summary>
        /// <param name="c1">The left color (interp == 0.0). Set to (59, 76, 192) for cold color</param>
        /// <param name="c2">The right color (interp == 1.0). Set to (180, 4, 38) for warm color</param>
        /// <param name="interp">The interpolation value between 0.0 and 1.0</param>
        public void FromColorInterpolation(Color c1, Color c2, float interp)
        {
            MSHColor m1 = new MSHColor(c1);
            MSHColor m2 = new MSHColor(c2);

            FromColorInterpolation(m1, m2, interp);
        }

        /// <summary>
        /// Make an interpolation from m1 to m2 
        /// </summary>
        /// <param name="m1">The left color (interp == 0.0). Set to RGB = (59, 76, 192) for cold color</param>
        /// <param name="m2">The right color (interp == 1.0). Set to RGB = (180, 4, 38) for warm color</param>
        /// <param name="interp">The interpolation value between 0.0 and 1.0</param>
        public void FromColorInterpolation(MSHColor m1, MSHColor m2, float interp)
        {
            float radDiff = Math.Abs(m1.H - m2.H);

            if(m1.S > 0.05 &&
               m2.S > 0.05 &&
               radDiff > Math.PI/3.0f)
            {
                float midM = m1.M;
                if (midM < m2.M)
                    midM = m2.M;
                if (midM < 98)
                    midM = 98;
                if(interp < 0.5f)
                {
                    m2.M = midM;
                    m2.S = 0;
                    m2.H = 0;
                    interp *= 2.0f;
                }
                else
                {
                    m1.M = midM;
                    m1.S = 0;
                    m1.H = 0;
                    interp = 2.0f*interp - 1.0f;
                }
            }

            if(m1.S < 0.05 && m2.S > 0.05)
                m1.H = AdjustHue(m2, m1.M);
            else if(m1.S > 0.05 && m2.S < 0.05)
                m2.H = AdjustHue(m1, m2.M);

            MSHColor mid = m1*(1.0f-interp) + m2*(interp);
            M = mid.M;
            S = mid.S;
            H = mid.H;
        }

        /// <summary>
        /// Convert a RGB colorspace value into a MSH colorspace value
        /// </summary>
        /// <param name="color">The RGB value</param>
        public void FromRGB(Color color)
        {
            FromLABColor(new LABColor(color));
        }

        /// <summary>
        /// Convert a XYZ colorspace value into a MSH colorspace value
        /// </summary>
        /// <param name="color">The XYZ value</param>
        public void FromXYZ(XYZColor color)
        {
            FromLABColor(new LABColor(color));
        }

        /// <summary>
        /// Convert a CIELAB colorspace value into a MSH colorspace value
        /// </summary>
        /// <param name="color">The CIELAB color space value</param>
        public void FromLABColor(LABColor color)
        {
            M = (float)Math.Sqrt(color.L*color.L + color.A*color.A + color.B*color.B);
            S = (float)Math.Acos(color.L/M);
            H = (float)Math.Atan2(color.B, color.A);
            A = color.Transparency;
        }

        /// <summary>
        /// Convert a MSH colorspace value into a CIELAB colorspace value
        /// </summary>
        /// <returns>The CIELAB colorspace value</returns>
        public LABColor ToLABColor()
        {
            float l = (float)(M * Math.Cos(S));
            float a = (float)(M * Math.Sin(S) * Math.Cos(H));
            float b = (float)(M * Math.Sin(S) * Math.Sin(H));
            return new LABColor(l, a, b, A);
        }

        /// <summary>
        /// Convert a MSH colorspace value into a XYZ colorspace value
        /// </summary>
        /// <returns>The XYZ colorspace value</returns>
        public XYZColor ToXYZ()
        {
            return ToLABColor().ToXYZ();
        }

        /// <summary>
        /// Make a linear interpolation between two LAB Color
        /// </summary>
        /// <param name="c1">the first color (t=0)</param>
        /// <param name="c2">the second color (t=1)</param>
        /// <param name="t">the progression factor (between 0 and 1)</param>
        /// <returns>The interpolated color</returns>
        public static MSHColor Lerp(MSHColor c1, MSHColor c2, float t)
        {
            return new MSHColor(c1*(1.0f-t) + c2*t);
        }

        /// <summary>
        /// Multiply the components of a color by a constant
        /// </summary>
        /// <param name="c1">The color</param>
        /// <param name="t">The constant</param>
        /// <returns>c1*t</returns>
        public static MSHColor operator*(MSHColor c1, float t)
        {
            return new MSHColor(c1.M*t, c1.S*t, c1.H*t, c1.A*t);
        }

        /// <summary>
        /// Multiply the components of a color by a constant
        /// </summary>
        /// <param name="t">The constant</param>
        /// <param name="c1">The color</param>
        /// <returns>c1*t</returns>
        public static MSHColor operator*(float t, MSHColor c1)
        {
            return c1 * t;
        }

        /// <summary>
        /// Apply - operation on colors components
        /// </summary>
        /// <param name="c1">The left operation color</param>
        /// <param name="c2">The right operation color</param>
        /// <returns>c1-c2</returns>
        public static MSHColor operator-(MSHColor c1, MSHColor c2)
        {
            return new MSHColor(c1.M-c2.M, c1.S-c2.S, c1.H-c2.H, c1.A-c2.A);
        }

        /// <summary>
        /// Apply + operation on colors components
        /// </summary>
        /// <param name="c1">The left operation color</param>
        /// <param name="c2">The right operation color</param>
        /// <returns>c1+c2</returns>
        public static MSHColor operator +(MSHColor c1, MSHColor c2)
        {
            return new MSHColor(c1.M+c2.M, c1.S+c2.S, c1.H+c2.H, c1.A+c2.A);
        }

        /// <summary>
        /// Adjust the Hue
        /// </summary>
        /// <param name="color">The saturated color</param>
        /// <param name="m">The unsaturated M component</param>
        /// <returns></returns>
        public static float AdjustHue(MSHColor color, float m)
        {
            if(color.M >= m)
                return color.H;

            float hSpin = (float)(color.S * Math.Sqrt(m*m - color.M*color.M) / (color.M*Math.Sin(color.S)));
            if(hSpin > -Math.PI/3.0)
                return color.H + hSpin;
            return color.H - hSpin;
        }

        /// <summary>
        /// M Component
        /// </summary>
        public float M { get; set;}

        /// <summary>
        /// S Component
        /// </summary>
        public float S { get; set;}

        /// <summary>
        /// L Component
        /// </summary>
        public float H { get; set;}

        /// <summary>
        /// A Component
        /// </summary>
        public float A { get; set;}
    }
}
