namespace UglyToad.PdfPig.Tests.Dla
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.DocumentLayoutAnalysis;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
    using UglyToad.PdfPig.Core;

    public class IntervalRelationsHelperTests
    {
        // Note (0,0) is bottom left of page

        /// <summary>
        /// A is equal to B.
        /// <para>----------|____A____|------------</para>
        /// <para>----------|____B____|------------</para>
        /// </summary>
        [Fact]
        public void IntervalRelation_Equals_X()
        {
            var a = new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(10, 10));

            var res = IntervalRelationsHelper.GetRelationX(a, a, 5);

            Assert.Equal(IntervalRelations.Equals, res);
        }

        [Fact]
        public void IntervalRelation_Equals_Y()
        {
            var a = new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(10, 10));

            var res = IntervalRelationsHelper.GetRelationY(a, a, 5);

            Assert.Equal(IntervalRelations.Equals, res);
        }

        /// <summary>
        /// Precedes: A takes place before B.
        /// <para>|____A____|----------------------</para>
        /// <para>----------------------|____B____|</para>
        /// </summary>
        /// 
        [Fact]
        public void IntervalRelation_Precedes_X()
        {
            var a = PdfPointTestExtensions.BoxAtTopLeft();
            var b = PdfPointTestExtensions.BoxAtTopLeft().MoveLeft(100);

            var res = IntervalRelationsHelper.GetRelationX(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationX(b, a, 5);

            Assert.Equal(IntervalRelations.Precedes, res);
            Assert.Equal(IntervalRelations.PrecedesI, resInverse);
        }

        [Fact]
        public void IntervalRelation_Precedes_Y()
        {
            var a = PdfPointTestExtensions.BoxAtTopLeft();
            var b = a.MoveDown(200);

            var res = IntervalRelationsHelper.GetRelationY(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationY(b, a, 5);

            Assert.Equal(IntervalRelations.Precedes, res);
            Assert.Equal(IntervalRelations.PrecedesI, resInverse);
        }


        /// <summary>
        /// A meets B.
        /// <para>|_____A______|--------------</para>
        /// <para>--------------|______B_____|</para>
        /// </summary>
        [Fact]
        public void IntervalRelation_Meets_X()
        {
            var a = PdfPointTestExtensions.BoxAtTopLeft(100);
            var b = a.MoveLeft(100);

            var res = IntervalRelationsHelper.GetRelationX(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationX(b, a, 5);

            Assert.Equal(IntervalRelations.Meets, res);
            Assert.Equal(IntervalRelations.MeetsI, resInverse);
        }        
        
        /// <summary>
        /// A meets B.
        /// <para>|_____A______|--------------</para>
        /// <para>--------------|______B_____|</para>
        /// </summary>
        [Fact]
        public void IntervalRelation_Meets_X_WithinTolerance()
        {
            var a = PdfPointTestExtensions.BoxAtTopLeft(100);
            var b = a.MoveLeft(110);

            var res = IntervalRelationsHelper.GetRelationX(a, b, 11);
            var resInverse = IntervalRelationsHelper.GetRelationX(b, a, 11);

            Assert.Equal(IntervalRelations.Meets, res);
            Assert.Equal(IntervalRelations.MeetsI, resInverse);
        }

        [Fact]
        public void IntervalRelation_Meets_Y()
        {
            var a = PdfPointTestExtensions.BoxAtTopLeft(100);
            var b = a.MoveDown(100);

            var res = IntervalRelationsHelper.GetRelationY(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationY(b, a, 5);

            Assert.Equal(IntervalRelations.Meets, res);
            Assert.Equal(IntervalRelations.MeetsI, resInverse);
        }


        [Fact]
        public void IntervalRelation_Meets_Y_WhenMovedDown_BecomesPreceeds()
        {
            // We take an A B that meets and move the B further down so becomes preceeds
            var startPoint = new PdfPoint(100, 600);
            var a = new PdfRectangle(startPoint, startPoint.MoveDown(100));
            var meetsABox = a.MoveDown(100);

            var res = IntervalRelationsHelper.GetRelationY(a, meetsABox, 5);
            var resInverse = IntervalRelationsHelper.GetRelationY(meetsABox, a, 5);

            Assert.Equal(IntervalRelations.Meets, res);
            Assert.Equal(IntervalRelations.MeetsI, resInverse);
            
            var preceededByABox = meetsABox.MoveDown(100);


            var moveRes = IntervalRelationsHelper.GetRelationY(a, preceededByABox, 5);
            var moveResInverse = IntervalRelationsHelper.GetRelationY(preceededByABox, a, 5);

            Assert.Equal(IntervalRelations.Precedes, moveRes);
            Assert.Equal(IntervalRelations.PrecedesI, moveResInverse);
        }

        /// <summary>
        /// A overlaps with B.
        /// <para>|________A________|-------------</para>
        /// <para>-------------|________B________|</para>
        /// </summary>
        [Fact]
        public void IntervalRelation_Overlaps_X()
        {
            var a = PdfPointTestExtensions.BoxAtTopLeft(100);
            var b = a.MoveLeft(a.Width/2);

            var res = IntervalRelationsHelper.GetRelationX(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationX(b, a, 5);

            Assert.Equal(IntervalRelations.Overlaps, res);
            Assert.Equal(IntervalRelations.OverlapsI, resInverse);
        }

        [Fact]
        public void IntervalRelation_Overlaps_Y()
        {
            var a = PdfPointTestExtensions.BoxAtTopLeft(100);
            var b = a.MoveLeft(500).MoveDown(a.Height / 2); // Only the move down is important

            var res = IntervalRelationsHelper.GetRelationY(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationY(b, a, 5);

            Assert.Equal(IntervalRelations.Overlaps, res);
            Assert.Equal(IntervalRelations.OverlapsI, resInverse);
        }

        /// <summary>
        /// A starts B.
        /// <para>|____A____|-----------------</para>
        /// <para>|_______B_______|-----------</para>
        /// </summary>
        [Fact]
        public void IntervalRelation_Starts_X()
        {
            var topLeft = PdfPointTestExtensions.OriginTopLeft();
            var a = new PdfRectangle(topLeft, topLeft.MoveLeft(50).MoveDown(10));
            var b = new PdfRectangle(topLeft, topLeft.MoveLeft(100).MoveDown(10));

            var res = IntervalRelationsHelper.GetRelationX(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationX(b, a, 5);

            Assert.Equal(IntervalRelations.Starts, res);
            Assert.Equal(IntervalRelations.StartsI, resInverse);
        }

        [Fact]
        public void IntervalRelation_Starts_Y()
        {
            var topLeft = PdfPointTestExtensions.OriginTopLeft();
            var a = new PdfRectangle(topLeft, topLeft.MoveLeft(100).MoveDown(100));
            var b = new PdfRectangle(topLeft, topLeft.MoveLeft(100).MoveDown(200));

            var res = IntervalRelationsHelper.GetRelationY(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationY(b, a, 5);

            Assert.Equal(IntervalRelations.Starts, res);
            Assert.Equal(IntervalRelations.StartsI, resInverse);
        }

        /// <summary>
        /// A during B.
        /// <para>--------|____A____|---------</para>
        /// <para>-----|_______B________|-----</para>
        /// </summary>
        ///During,
        [Fact]
        public void IntervalRelation_During_X()
        {
            var a = new PdfRectangle(new PdfPoint(20, 0), new PdfPoint(80, 0));
            var b = new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(100, 0));

            var res = IntervalRelationsHelper.GetRelationX(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationX(b, a, 5);

            Assert.Equal(IntervalRelations.During, res);
            Assert.Equal(IntervalRelations.DuringI, resInverse);
        }

        [Fact]
        public void IntervalRelation_During_Y()
        {
            var a = new PdfRectangle(new PdfPoint(0, 20), new PdfPoint(0, 80));
            var b = new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(0, 100));

            var res = IntervalRelationsHelper.GetRelationY(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationY(b, a, 5);

            Assert.Equal(IntervalRelations.During, res);
            Assert.Equal(IntervalRelations.DuringI, resInverse);
        }

        /// <summary>
        /// A finishes B.
        /// <para>-----------------|____A____|</para>
        /// <para>-----------|_______B_______|</para>
        /// </summary>
        [Fact]
        public void IntervalRelation_Finishes_X()
        {
            var topRight = PdfPointTestExtensions.OriginTopLeft().MoveLeft(400);
            var a = new PdfRectangle(topRight.MoveX(-100), topRight);
            var b = new PdfRectangle(topRight.MoveX(-200), topRight);

            var res = IntervalRelationsHelper.GetRelationX(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationX(b, a, 5);

            Assert.Equal(IntervalRelations.Finishes, res);
            Assert.Equal(IntervalRelations.FinishesI, resInverse);
        }

        [Fact]
        public void IntervalRelation_Finishes_Y()
        {
            var topleft = PdfPointTestExtensions.OriginTopLeft();
            var a = PdfPointTestExtensions.BoxAtTopLeft(20).MoveDown(20);
            var b = PdfPointTestExtensions.BoxAtTopLeft(40);

            var res = IntervalRelationsHelper.GetRelationY(a, b, 5);
            var resInverse = IntervalRelationsHelper.GetRelationY(b, a, 5);

            Assert.Equal(IntervalRelations.Finishes, res);
            Assert.Equal(IntervalRelations.FinishesI, resInverse);
        }
    }

    internal static class PdfPointTestExtensions
    {

        internal static PdfPoint OriginTopLeft()
        {
            return new PdfPoint(0, 800);
        }

        internal static PdfPoint MoveLeft(this PdfPoint it, double dist)
        {
            if (dist < 0) throw new ArgumentException(nameof(dist) + "must be positive");

            return it.MoveX(dist);
        }
        internal static PdfPoint MoveDown(this PdfPoint it, double dist)
        {
            if (dist < 0) throw new ArgumentException(nameof(dist) + "must be positive");

            return it.MoveY(-dist);
        }

        internal static PdfRectangle BoxAtTopLeft(double length = 10d)
        {
            return new PdfRectangle(OriginTopLeft(), OriginTopLeft().MoveLeft(length).MoveDown(length));
        }


        internal static PdfRectangle MoveLeft(this PdfRectangle start, double dist)
        {
            if (dist < 0) throw new ArgumentException(nameof(dist) + "must be positive");

            return new PdfRectangle(start.BottomLeft.MoveLeft(dist), start.TopRight.MoveLeft(dist));
        }



        internal static PdfRectangle MoveDown(this PdfRectangle start, double dist)
        {
            if (dist < 0) throw new ArgumentException(nameof(dist) + "must be positive");

            return new PdfRectangle(start.BottomLeft.MoveDown(dist), start.TopRight.MoveDown(dist));
        }
    }
}
