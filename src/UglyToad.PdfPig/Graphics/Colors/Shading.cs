namespace UglyToad.PdfPig.Graphics.Colors
{
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// A shading specifies details of a particular gradient fill, including the type
    /// of shading to be used, the geometry of the area to be shaded, and the geometry of the
    /// gradient fill. Various shading types are available, depending on the value of the
    /// dictionary's ShadingType entry.
    /// </summary>
    public abstract class Shading
    {
        /// <summary>
        /// The dictionary defining the shading.
        /// </summary>
        public DictionaryToken ShadingDictionary { get; }

        /// <summary>
        /// The shading type.
        /// </summary>
        public ShadingType ShadingType { get; }

        /// <summary>
        /// The colour space in which colour values shall beexpressed.
        /// This may be any device, CIE-based, or special colour space except a Pattern space.
        /// </summary>
        public ColorSpaceDetails ColorSpace { get; }

        /// <summary>
        /// An array of colour components appropriate to the colour space,
        /// specifying a single background colour value. If present, this
        /// colour shall be used, before any painting operation involving
        /// the shading, to fill those portions of the area to be painted
        /// that lie outside the bounds of the shading object.
        /// </summary>
        public double[] Background { get; }

        /// <summary>
        /// The shading's bounding box. The coordinates shall be interpreted
        /// in the shading's target coordinate space. If present, this bounding
        /// box shall be applied as a temporary clipping boundary when the shading
        /// is painted, in addition to the current clipping path and any other
        /// clipping boundaries in effect at that time.
        /// </summary>
        public PdfRectangle? BBox { get; }

        /// <summary>
        /// The shading operators sample shading functions at a rate determined by
        /// the resolution of the output device. Aliasing can occur if the function
        /// is not smooth—that is, if it has a high spatial frequency relative to
        /// the sampling rate. Anti-aliasing can be computationally expensive and 
        /// is usually unnecessary, since most shading functions are smooth enough
        /// or are sampled at a high enough frequency to avoid aliasing effects.
        /// Anti-aliasing may not be implemented on some output devices, in which
        /// case this flag is ignored.
        /// </summary>
        public bool AntiAlias { get; }

        /// <summary>
        /// Create a new <see cref="Shading"/>.
        /// </summary>
        protected internal Shading(ShadingType shadingType, bool antiAlias, DictionaryToken shadingDictionary,
            ColorSpaceDetails colorSpace, PdfRectangle? bbox, double[] background)
        {
            ShadingType = shadingType;
            AntiAlias = antiAlias;
            ShadingDictionary = shadingDictionary;
            ColorSpace = colorSpace;
            BBox = bbox;
            Background = background;
        }

        /// <summary>
        /// The shading's function(s), if any.
        /// </summary>
        public abstract PdfFunction[] Functions { get; }

        /// <summary>
        /// Convert the input values using the functions of the shading.
        /// </summary>
        public double[] Eval(params double[] input)
        {
            if (Functions == null || Functions.Length == 0)
            {
                return input;
            }
            else if (Functions.Length == 1)
            {
                return Clamp(Functions[0].Eval(input));
            }

            double[] returnValues = new double[Functions.Length];
            for (int i = 0; i < Functions.Length; i++)
            {
                double[] newValue = Functions[i].Eval(input);
                returnValues[i] = newValue[0]; // 1-out functions
            }
            return Clamp(returnValues);
        }

        private static double[] Clamp(double[] input)
        {
            // From the PDF spec:
            // "If the value returned by the function for a given colour component 
            // is out of range, it shall be adjusted to the nearest valid value."
            for (int i = 0; i < input.Length; ++i)
            {
                if (input[i] < 0)
                {
                    input[i] = 0;
                }
                else if (input[i] > 1)
                {
                    input[i] = 1;
                }
            }
            return input;
        }
    }

    /// <summary>
    /// Function-based shadings (type 1) define the colour of every point in the domain using a
    /// mathematical function (not necessarily smooth or continuous).
    /// </summary>
    public sealed class FunctionBasedShading : Shading
    {
        /// <summary>
        /// (Optional) An array of four numbers [xmin xmax ymin ymax] specifying the rectangular domain of
        /// coordinates over which the colour function(s) are defined.
        /// <para>
        /// Default value: [0.0 1.0 0.0 1.0].
        /// </para>
        /// </summary>
        public double[] Domain { get; }

        /// <summary>
        /// (Optional) An array of six numbers specifying a transformation matrix mapping the coordinate
        /// space specified by the Domain entry into the shading's target coordinate space.
        /// <para>
        /// Default value: the identity matrix [1 0 0 1 0 0].
        /// </para>
        /// </summary>
        public TransformationMatrix Matrix { get; }

        /// <summary>
        /// (Required) A 2-in, n-out function or an array of n 2-in, 1-out functions (where n is the
        /// number of colour components in the shading dictionary's colour space).
        /// Each function's domain shall be a superset of that of the shading dictionary.
        /// If the value returned by the function for a given colour component is out of
        /// range, it shall be adjusted to the nearest valid value.
        /// </summary>
        public override PdfFunction[] Functions { get; }

        /// <summary>
        /// Create a new <see cref="FunctionBasedShading"/>.
        /// </summary>
        public FunctionBasedShading(bool antiAlias, DictionaryToken shadingDictionary,
            ColorSpaceDetails colorSpace, PdfRectangle? bbox, double[] background, double[] domain,
            TransformationMatrix matrix, PdfFunction[] functions)
            : base(ShadingType.FunctionBased, antiAlias, shadingDictionary, colorSpace, bbox, background)
        {
            Domain = domain;
            Matrix = matrix;
            Functions = functions;
        }
    }

    /// <summary>
    /// Axial shadings (type 2) define a colour blend along a line between two points, optionally
    /// extended beyond the boundary points by continuing the boundary colours.
    /// </summary>
    public sealed class AxialShading : Shading
    {
        /// <summary>
        /// (Required) An array of four numbers [x0 y0 x1 y1] specifying the starting and ending coordinates
        /// of the axis, expressed in the shading's target coordinate space.
        /// </summary>
        public double[] Coords { get; }

        /// <summary>
        /// (Optional) An array of two numbers [t0 t1] specifying the limiting values of a parametric variable t.
        /// The variable is considered to vary linearly between these two values as the colour gradient
        /// varies between the starting and ending points of the axis. The variable t becomes the input
        /// argument to the colour function(s). Default value: [0.0 1.0].
        /// </summary>
        public double[] Domain { get; }

        /// <summary>
        /// (Required) A 1-in, n-out function or an array of n 1-in, 1-out functions (where n is the number of
        /// colour components in the shading dictionary's colour space). The function(s) shall be
        /// called with values of the parametric variable t in the domain defined by the Domain entry.
        /// Each function's domain shall be a superset of that of the shading dictionary. If the value
        /// returned by the function for a given colour component is out of range, it shall be adjusted
        /// to the nearest valid value.
        /// </summary>
        public override PdfFunction[] Functions { get; }

        /// <summary>
        /// (Optional) An array of two boolean values specifying whether to extend the shading beyond the starting
        /// and ending points of the axis, respectively. Default value: [false false].
        /// </summary>
        public bool[] Extend { get; }

        /// <summary>
        /// Create a new <see cref="AxialShading"/>.
        /// </summary>
        public AxialShading(bool antiAlias, DictionaryToken shadingDictionary,
            ColorSpaceDetails colorSpace, PdfRectangle? bbox, double[] background,
            double[] coords, double[] domain, PdfFunction[] functions, bool[] extend)
            : base(ShadingType.Axial, antiAlias, shadingDictionary, colorSpace, bbox, background)
        {
            Coords = coords;
            Domain = domain;
            Functions = functions;
            Extend = extend;
        }
    }

    /// <summary>
    /// Radial shadings (type 3) define a blend between two circles, optionally extended beyond the
    /// boundary circles by continuing the boundary colours. This type of shading is commonly used
    /// to represent three-dimensional spheres and cones.
    /// </summary>
    public sealed class RadialShading : Shading
    {
        /// <summary>
        /// (Required) An array of six numbers [x0 y0 r0 x1 y1 r1] specifying the centres and radii of the starting
        /// and ending circles, expressed in the shading's target coordinate space. The radii r0 and r1
        /// shall both be greater than or equal to 0. If one radius is 0, the corresponding circle shall
        /// be treated as a point; if both are 0, nothing shall be painted.
        /// </summary>
        public double[] Coords { get; }

        /// <summary>
        /// (Optional) An array of two numbers [t0 t1] specifying the limiting values of a parametric variable t.
        /// The variable is considered to vary linearly between these two values as the colour gradient
        /// varies between the starting and ending circles. The variable t becomes the input argument
        /// to the colour function(s).
        /// <para>
        /// Default value: [0.0 1.0].
        /// </para>
        /// </summary>
        public double[] Domain { get; }

        /// <summary>
        /// (Required) A 1-in, n-out function or an array of n 1-in, 1-out functions (where n is the number of
        /// colour components in the shading dictionary's colour space). The function(s) shall be
        /// called with values of the parametric variable t in the domain defined by the shading
        /// dictionary's Domain entry. Each function’s domain shall be a superset of that of the
        /// shading dictionary. If the value returned by the function for a given colour component
        /// is out of range, it shall be adjusted to the nearest valid value.
        /// </summary>
        public override PdfFunction[] Functions { get; }

        /// <summary>
        /// (Optional) An array of two boolean values specifying whether to extend the shading beyond the starting
        /// and ending points of the axis, respectively.
        /// <para>
        /// Default value: [false false].
        /// </para>
        /// </summary>
        public bool[] Extend { get; }

        /// <summary>
        /// Create a new <see cref="RadialShading"/>.
        /// </summary>
        public RadialShading(bool antiAlias, DictionaryToken shadingDictionary,
            ColorSpaceDetails colorSpace, PdfRectangle? bbox, double[] background,
            double[] coords, double[] domain, PdfFunction[] functions, bool[] extend)
            : base(ShadingType.Radial, antiAlias, shadingDictionary, colorSpace, bbox, background)
        {
            Coords = coords;
            Domain = domain;
            Functions = functions;
            Extend = extend;
        }
    }

    /// <summary>
    /// Free-form Gouraud-shaded triangle meshes (type 4) define a common construct used by many
    /// three-dimensional applications to represent complex coloured and shaded shapes. Vertices
    /// are specified in free-form geometry.
    /// </summary>
    public sealed class FreeFormGouraudShading : Shading
    {
        /// <summary>
        /// (Required) The number of bits used to represent each vertex coordinate.
        /// The value shall be 1, 2, 4, 8, 12, 16, 24, or 32.
        /// </summary>
        public int BitsPerCoordinate { get; }

        /// <summary>
        /// (Required) The number of bits used to represent each colour component.
        /// The value shall be 1, 2, 4, 8, 12, or 16.
        /// </summary>
        public int BitsPerComponent { get; }

        /// <summary>
        /// (Required) The number of bits used to represent the edge flag for each vertex (see below).
        /// The value of BitsPerFlag shall be 2, 4, or8, but only the least significant 2 bits
        /// in each flag value shall beused. The value for the edge flag shall be 0, 1, or 2.
        /// </summary>
        public int BitsPerFlag { get; }

        /// <summary>
        /// (Required) An array of numbers specifying how to map vertex coordinates and colour components
        /// into the appropriate ranges of values. The decoding method is similar to that used
        /// in image dictionaries. The ranges shall bespecified as follows:
        /// [xmin xmax ymin ymax c1, min c1, max … cn, min cn, max]
        /// Only one pair of c values shall be specified if a Function entry is present.
        /// </summary>
        public double[] Decode { get; }

        /// <summary>
        /// (Optional) A 1-in, n-out function or an array of n 1-in, 1-out functions (where n
        /// is the number of colour components in the shading dictionary's colour space). If
        /// this entry is present, the colour data for each vertex shall be specified by a
        /// single parametric variable rather than by n separate colour components.
        /// The designated function(s) shall be called with each interpolated value of the
        /// parametric variable to determine the actual colour at each point. Each input
        /// value shall be forced into the range interval specified for the corresponding
        /// colour component in the shading dictionary's Decode array. Each function’s
        /// domain shall be a superset of that interval. If the value returned by the
        /// function for a given colour component is out of range, it shall be adjusted
        /// to the nearest valid value.
        /// This entry shall not be used with an Indexed colour space.
        /// </summary>
        public override PdfFunction[] Functions { get; }

        /// <summary>
        /// Create a new <see cref="FreeFormGouraudShading"/>.
        /// </summary>
        public FreeFormGouraudShading(bool antiAlias, StreamToken shadingStream,
            ColorSpaceDetails colorSpace, PdfRectangle? bbox, double[] background,
            int bitsPerCoordinate, int bitsPerComponent, int bitsPerFlag, double[] decode, PdfFunction[] functions)
            : base(ShadingType.FreeFormGouraud, antiAlias, shadingStream.StreamDictionary, colorSpace, bbox, background)
        {
            BitsPerCoordinate = bitsPerCoordinate;
            BitsPerComponent = bitsPerComponent;
            BitsPerFlag = bitsPerFlag;
            Decode = decode;
            Functions = functions;
        }
    }

    /// <summary>
    /// Lattice-form Gouraud-shaded triangle meshes (type 5) are based on the same geometrical
    /// construct as type 4 but with vertices specified as a pseudorectangular lattice.
    /// </summary>
    public sealed class LatticeFormGouraudShading : Shading
    {
        /// <summary>
        /// (Required) The number of bits used to represent each vertex coordinate.
        /// The value shall be 1, 2, 4, 8, 12, 16, 24, or 32.
        /// </summary>
        public int BitsPerCoordinate { get; }

        /// <summary>
        /// (Required) The number of bits used to represent each colour component.
        /// The value shall be 1, 2, 4, 8, 12, or 16.
        /// </summary>
        public int BitsPerComponent { get; }

        /// <summary>
        /// (Required) The number of vertices in each row of the lattice; the value shall be greater than
        /// or equal to 2. The number of rows need not be specified.
        /// </summary>
        public int VerticesPerRow { get; }

        /// <summary>
        /// (Required) An array of numbers specifying how to map vertex coordinates and colour components
        /// into the appropriate ranges of values. The decoding method is similar to that used
        /// in image dictionaries. The ranges shall bespecified as follows:
        /// [xmin xmax ymin ymax c1, min c1, max … cn, min cn, max]
        /// Only one pair of c values shall be specified if a Function entry is present.
        /// </summary>
        public double[] Decode { get; }

        /// <summary>
        /// (Optional) A 1-in, n-out function or an array of n 1-in, 1-out functions (where n is the number
        /// of colour components in the shading dictionary's colour space). If this entry is present,
        /// the colour data for each vertex shall be specified by a single parametric variable rather
        /// than by n separate colour components. The designated function(s) shall be called with each
        /// interpolated value of the parametric variable to determine the actual colour at each point.
        /// Each input value shall be forced into the range interval specified for the corresponding
        /// colour component in the shading dictionary's Decode array. Each function's domain shall be
        /// a superset of that interval. If the value returned by the function for a given colour
        /// component is out of range, it shall be adjusted to the nearest valid value.
        /// This entry shall not be used with an Indexed colour space.
        /// </summary>
        public override PdfFunction[] Functions { get; }

        /// <summary>
        /// Create a new <see cref="LatticeFormGouraudShading"/>.
        /// </summary>
        public LatticeFormGouraudShading(bool antiAlias, StreamToken shadingStream,
            ColorSpaceDetails colorSpace, PdfRectangle? bbox, double[] background,
            int bitsPerCoordinate, int bitsPerComponent, int verticesPerRow, double[] decode, PdfFunction[] functions)
            : base(ShadingType.LatticeFormGouraud, antiAlias, shadingStream.StreamDictionary, colorSpace, bbox, background)
        {
            BitsPerCoordinate = bitsPerCoordinate;
            BitsPerComponent = bitsPerComponent;
            VerticesPerRow = verticesPerRow;
            Decode = decode;
            Functions = functions;
        }
    }

    /// <summary>
    /// Coons patch meshes (type 6) construct a shading from one or more colour patches, each
    /// bounded by four cubic Bézier curves.
    /// </summary>
    public class CoonsPatchMeshesShading : Shading
    {
        /// <summary>
        /// (Required) The number of bits used to represent each vertex coordinate.
        /// The value shall be 1, 2, 4, 8, 12, 16, 24, or 32.
        /// </summary>
        public int BitsPerCoordinate { get; }

        /// <summary>
        /// (Required) The number of bits used to represent each colour component.
        /// The value shall be 1, 2, 4, 8, 12, or 16.
        /// </summary>
        public int BitsPerComponent { get; }

        /// <summary>
        /// (Required) The number of bits used to represent the edge flag for each patch (see below).
        /// The value shall be 2, 4, or 8, but only the least significant 2 bits in each flag
        /// value shall be used. Valid values for the edge flag shall be 0, 1, 2, and 3.
        /// </summary>
        public int BitsPerFlag { get; }

        /// <summary>
        /// (Required) An array of numbers specifying how to map coordinates and colour components into the
        /// appropriate ranges of values. The decoding method is similar to that used in image
        /// dictionaries. The ranges shall be specified as follows:
        /// [xmin xmax ymin ymax c1, min c1, max … cn, min cn, max]
        /// Only one pair of c values shall be specified if a Function entry is present.
        /// </summary>
        public double[] Decode { get; }

        /// <summary>
        /// (Optional) A 1-in, n-out function or an array of n 1-in, 1-out functions (where n is
        /// the number of colour components in the shading dictionary's colour space). If this
        /// entry is present, the colour data for each vertex shall be specified by a single
        /// parametric variable rather than by n separate colour components. The designated
        /// function(s) shall be called with each interpolated value of the parametric variable
        /// to determine the actual colour at each point. Each input value shall be forced into
        /// the range interval specified for the corresponding colour component in the shading
        /// dictionary's Decode array. Each function’s domain shall be a superset of that interval.
        /// If the value returned by the function for a given colour component is out of range, it
        /// shall be adjusted to the nearest valid value.
        /// This entry shall not be used with an Indexed colour space.
        /// </summary>
        public override PdfFunction[] Functions { get; }

        /// <summary>
        /// Create a new <see cref="CoonsPatchMeshesShading"/>.
        /// </summary>
        public CoonsPatchMeshesShading(bool antiAlias, StreamToken shadingStream,
            ColorSpaceDetails colorSpace, PdfRectangle? bbox, double[] background,
            int bitsPerCoordinate, int bitsPerComponent, int bitsPerFlag, double[] decode, PdfFunction[] functions)
            : base(ShadingType.CoonsPatch, antiAlias, shadingStream.StreamDictionary, colorSpace, bbox, background)
        {
            BitsPerCoordinate = bitsPerCoordinate;
            BitsPerComponent = bitsPerComponent;
            BitsPerFlag = bitsPerFlag;
            Decode = decode;
            Functions = functions;
        }
    }

    /// <summary>
    /// Tensor-product patch meshes (type 7) are similar to type 6 but with additional control
    /// points in each patch, affording greater control over colour mapping.
    /// </summary>
    public sealed class TensorProductPatchMeshesShading : Shading
    {
        /// <summary>
        /// (Required) The number of bits used to represent each vertex coordinate.
        /// The value shall be 1, 2, 4, 8, 12, 16, 24, or 32.
        /// </summary>
        public int BitsPerCoordinate { get; }

        /// <summary>
        /// (Required) The number of bits used to represent each colour component.
        /// The value shall be 1, 2, 4, 8, 12, or 16.
        /// </summary>
        public int BitsPerComponent { get; }

        /// <summary>
        /// (Required) The number of bits used to represent the edge flag for each patch (see below).
        /// The value shall be 2, 4, or 8, but only the least significant 2 bits in each flag
        /// value shall be used. Valid values for the edge flag shall be 0, 1, 2, and 3.
        /// </summary>
        public int BitsPerFlag { get; }

        /// <summary>
        /// (Required) An array of numbers specifying how to map coordinates and colour components into the
        /// appropriate ranges of values. The decoding method is similar to that used in image
        /// dictionaries. The ranges shall be specified as follows:
        /// [xmin xmax ymin ymax c1, min c1, max … cn, min cn, max]
        /// Only one pair of c values shall be specified if a Function entry is present.
        /// </summary>
        public double[] Decode { get; }

        /// <summary>
        /// (Optional) A 1-in, n-out function or an array of n 1-in, 1-out functions (where n is
        /// the number of colour components in the shading dictionary's colour space). If this
        /// entry is present, the colour data for each vertex shall be specified by a single
        /// parametric variable rather than by n separate colour components. The designated
        /// function(s) shall be called with each interpolated value of the parametric variable
        /// to determine the actual colour at each point. Each input value shall be forced into
        /// the range interval specified for the corresponding colour component in the shading
        /// dictionary's Decode array. Each function’s domain shall be a superset of that interval.
        /// If the value returned by the function for a given colour component is out of range, it
        /// shall be adjusted to the nearest valid value.
        /// This entry shall not be used with an Indexed colour space.
        /// </summary>
        public override PdfFunction[] Functions { get; }

        /// <summary>
        /// Create a new <see cref="TensorProductPatchMeshesShading"/>.
        /// </summary>
        public TensorProductPatchMeshesShading(bool antiAlias, StreamToken shadingStream,
            ColorSpaceDetails colorSpace, PdfRectangle? bbox, double[] background,
            int bitsPerCoordinate, int bitsPerComponent, int bitsPerFlag, double[] decode, PdfFunction[] functions)
            : base(ShadingType.TensorProductPatch, antiAlias, shadingStream.StreamDictionary, colorSpace, bbox, background)
        {
            BitsPerCoordinate = bitsPerCoordinate;
            BitsPerComponent = bitsPerComponent;
            BitsPerFlag = bitsPerFlag;
            Decode = decode;
            Functions = functions;
        }
    }

    /// <summary>
    /// Shading types.
    /// </summary>
    public enum ShadingType : byte
    {
        /// <summary>
        /// Function-based shading.
        /// </summary>
        FunctionBased = 1,

        /// <summary>
        /// Axial shading.
        /// </summary>
        Axial = 2,

        /// <summary>
        /// Radial shading.
        /// </summary>
        Radial = 3,

        /// <summary>
        /// Free-form Gouraud-shaded triangle mesh.
        /// </summary>
        FreeFormGouraud = 4,

        /// <summary>
        /// Lattice-form Gouraud-shaded triangle mesh.
        /// </summary>
        LatticeFormGouraud = 5,

        /// <summary>
        /// Coons patch mesh.
        /// </summary>
        CoonsPatch = 6,

        /// <summary>
        /// Tensor-product patch mesh
        /// </summary>
        TensorProductPatch = 7
    }
}
