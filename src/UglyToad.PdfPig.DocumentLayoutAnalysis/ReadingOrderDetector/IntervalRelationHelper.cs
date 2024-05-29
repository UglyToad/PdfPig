namespace UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UglyToad.PdfPig.Core;

    /// <summary>
    /// Gets the Thick Boundary Rectangle Relations (TBRR) 
    /// <para>The Thick Boundary Rectangle Relations (TBRR) is a set of qualitative relations representing the spatial relations of the document objects on the page.
    /// For every pair of document objects a and b, one X and one Y interval relation hold. If one considers the pair in reversed
    /// order, the inverse interval relation holds. Therefore the directed graph g_i representing these relations is complete.</para>
    /// <para>See also https://en.wikipedia.org/wiki/Allen%27s_interval_algebra</para>
    /// </summary>
    public static class IntervalRelationsHelper
    {

        /// <summary>
        /// Gets the Thick Boundary Rectangle Relations (TBRR) for the X coordinate.
        /// <para>The Thick Boundary Rectangle Relations (TBRR) is a set of qualitative relations representing the spatial relations of the document objects on the page.
        /// For every pair of document objects a and b, one X and one Y interval relation hold. If one considers the pair in reversed
        /// order, the inverse interval relation holds. Therefore the directed graph g_i representing these relations is complete.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="T">The tolerance parameter T. If two coordinates are closer than T they are considered equal.</param>
        public static IntervalRelations GetRelationX(PdfRectangle a, PdfRectangle b, double T)
        {
            // Order is important
            if (b.Left - T <= a.Left && a.Left <= b.Left + T
                && (b.Right - T <= a.Right && a.Right <= b.Right + T))
            {
                return IntervalRelations.Equals;
            }

            if (b.Left - T <= a.Right
                && a.Right <= b.Left + T)
            {
                return IntervalRelations.Meets;
            }
            else if (a.Left - T <= b.Right
                && b.Right <= a.Left + T)
            {
                return IntervalRelations.MeetsI;
            }

            if (b.Left - T <= a.Left && a.Left <= b.Left + T
                && a.Right < b.Right - T)
            {
                return IntervalRelations.Starts;
            }
            else if (a.Left - T <= b.Left && b.Left <= a.Left + T
                && b.Right < a.Right - T)
            {
                return IntervalRelations.StartsI;
            }

            if (a.Left > b.Left + T
                && (b.Right - T <= a.Right && a.Right <= b.Right + T))
            {
                return IntervalRelations.Finishes;
            }
            else if (b.Left > a.Left + T
                && (a.Right - T <= b.Right && b.Right <= a.Right + T))
            {
                return IntervalRelations.FinishesI;
            }

            if (a.Left > b.Left + T
                && a.Right < b.Right - T)
            {
                return IntervalRelations.During;
            }
            else if (b.Left > a.Left + T
                && b.Right < a.Right - T)
            {
                return IntervalRelations.DuringI;
            }

            if (a.Left < b.Left - T
                && (b.Left + T < a.Right && a.Right < b.Right - T))
            {
                return IntervalRelations.Overlaps;
            }
            else if (b.Left < a.Left - T
                && (a.Left + T < b.Right && b.Right < a.Right - T))
            {
                return IntervalRelations.OverlapsI;
            }

            if (a.Right < b.Left - T)
            {
                return IntervalRelations.Precedes;
            }
            else if (b.Right < a.Left - T)
            {
                return IntervalRelations.PrecedesI;
            }

            return IntervalRelations.Unknown;
        }

        /// <summary>
        /// Gets the Thick Boundary Rectangle Relations (TBRR) for the Y coordinate.
        /// <para>The Thick Boundary Rectangle Relations (TBRR) is a set of qualitative relations representing the spatial relations of the document objects on the page.
        /// For every pair of document objects a and b, one X and one Y interval relation hold. If one considers the pair in reversed
        /// order, the inverse interval relation holds. Therefore the directed graph g_i representing these relations is complete.</para>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="T">The tolerance parameter T. If two coordinates are closer than T they are considered equal.</param>
        public static IntervalRelations GetRelationY(PdfRectangle a, PdfRectangle b, double T)
        {
            // Order is important
            if ((b.Top - T <= a.Top && a.Top <= b.Top + T)
                && (b.Bottom - T <= a.Bottom && a.Bottom <= b.Bottom + T))
            {
                return IntervalRelations.Equals;
            }

            if (a.Top - T <= b.Bottom
                && b.Bottom <= a.Top + T)
            {
                return IntervalRelations.MeetsI;
            }
            else if (b.Top - T <= a.Bottom
                && a.Bottom <= b.Top + T)
            {
                return IntervalRelations.Meets;
            }

            if (b.Top - T <= a.Top && a.Top <= b.Top + T
                && a.Bottom < b.Bottom - T)
            {
                return IntervalRelations.StartsI;
            }
            else if (a.Top - T <= b.Top && b.Top <= a.Top + T
                && b.Bottom < a.Bottom - T)
            {
                return IntervalRelations.Starts;
            }

            if (a.Top > b.Top + T
                && (b.Bottom - T <= a.Bottom && a.Bottom <= b.Bottom + T))
            {
                return IntervalRelations.FinishesI;
            }
            else if (b.Top > a.Top + T
                && (a.Bottom - T <= b.Bottom && b.Bottom <= a.Bottom + T))
            {
                return IntervalRelations.Finishes;
            }

            if (a.Top > b.Top + T
                && a.Bottom < b.Bottom - T)
            {
                return IntervalRelations.DuringI;
            }
            else if (b.Top > a.Top + T
                && b.Bottom < a.Bottom - T)
            {
                return IntervalRelations.During;
            }

            if (a.Top < b.Top - T
                && (b.Bottom + T < a.Top && a.Bottom < b.Bottom - T))
            {
                return IntervalRelations.OverlapsI;
            }
            else if (b.Top < a.Top - T
                && (a.Bottom + T < b.Top && b.Bottom < a.Bottom - T))
            {
                return IntervalRelations.Overlaps;
            }

            if (a.Bottom < b.Top - T)
            {
                return IntervalRelations.PrecedesI;
            }
            else if (b.Bottom < a.Top - T)
            {
                return IntervalRelations.Precedes;
            }

            return IntervalRelations.Unknown;
        }
    }

    /// <summary>
    /// Allen’s interval thirteen relations.
    /// <para>See https://en.wikipedia.org/wiki/Allen%27s_interval_algebra</para>
    /// </summary>
    public enum IntervalRelations
    {
        /// <summary>
        /// Unknown interval relations.
        /// </summary>
        Unknown,

        /// <summary>
        /// X takes place before Y.
        /// <para>|____X____|----------------------</para>
        /// <para>----------------------|____Y____|</para>
        /// </summary>
        Precedes,

        /// <summary>
        /// X meets Y.
        /// <para>|_____X______|--------------</para>
        /// <para>--------------|______Y_____|</para>
        /// </summary>
        Meets,

        /// <summary>
        /// X overlaps with Y.
        /// <para>|________X________|-------------</para>
        /// <para>-------------|________Y________|</para>
        /// </summary>
        Overlaps,

        /// <summary>
        /// X starts Y.
        /// <para>|____X____|-----------------</para>
        /// <para>|_______Y_______|-----------</para>
        /// </summary>
        Starts,

        /// <summary>
        /// X during Y.
        /// <para>--------|____X____|---------</para>
        /// <para>-----|_______Y________|-----</para>
        /// </summary>
        During,

        /// <summary>
        /// X finishes Y.
        /// <para>-----------------|____X____|</para>
        /// <para>-----------|_______Y_______|</para>
        /// </summary>
        Finishes,

        /// <summary>
        /// Inverse precedes.
        /// </summary>
        PrecedesI,

        /// <summary>
        /// Inverse meets.
        /// </summary>
        MeetsI,

        /// <summary>
        /// Inverse overlaps.
        /// </summary>
        OverlapsI,

        /// <summary>
        /// Inverse Starts.
        /// </summary>
        StartsI,

        /// <summary>
        /// Inverse during.
        /// </summary>
        DuringI,

        /// <summary>
        /// Inverse finishes.
        /// </summary>
        FinishesI,

        /// <summary>
        /// X is equal to Y.
        /// <para>----------|____X____|------------</para>
        /// <para>----------|____Y____|------------</para>
        /// </summary>
        Equals
    }
}
