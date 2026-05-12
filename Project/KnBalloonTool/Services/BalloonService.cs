using System;
using NXOpen;
using NXOpen.Annotations;

namespace KnBalloonTool.Services
{
    public static class BalloonService
    {
        private const double LeaderOffsetMm = 15.0;

        public static IdSymbol CreateDraftingBalloon(Part part, Dimension dim, string knNumber)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (dim == null) throw new ArgumentNullException(nameof(dim));
            if (string.IsNullOrEmpty(knNumber)) throw new ArgumentException("knNumber required");

            var builder = part.Annotations.IdSymbols.CreateIdSymbolBuilder(null);
            try
            {
                ConfigureStyle(builder.Style.IdSymbolStyle, knNumber);

                Point3d anchor = ComputeAnchor(dim);
                builder.Origin.Origin.SetValue(null, part.ModelingViews.WorkView, anchor);

                AddLeaderToDim(builder.Leader, dim);

                var created = builder.Commit() as IdSymbol;
                MappingService.MarkBalloonAsToolOwned(created, knNumber, dim);
                return created;
            }
            finally
            {
                builder.Destroy();
            }
        }

        public static PmiIdSymbol CreatePmiBalloon(Part part, PmiDimension dim, string knNumber)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (dim == null) throw new ArgumentNullException(nameof(dim));
            if (string.IsNullOrEmpty(knNumber)) throw new ArgumentException("knNumber required");

            var builder = part.PmiManager.PmiIdSymbols.CreatePmiIdSymbolBuilder(null);
            try
            {
                ConfigureStyle(builder.Style.IdSymbolStyle, knNumber);

                Point3d anchor = ComputePmiAnchor(dim);
                builder.Origin.Origin.SetValue(null, part.ModelingViews.WorkView, anchor);

                AddLeaderToPmi(builder.Leader, dim);

                var created = builder.Commit() as PmiIdSymbol;
                MappingService.MarkBalloonAsToolOwned(created, knNumber, dim);
                return created;
            }
            finally
            {
                builder.Destroy();
            }
        }

        private static void ConfigureStyle(IdSymbolStyleBuilder style, string knNumber)
        {
            style.Type = IdSymbolStyleBuilder.SymbolTypes.CircleType1;
            style.UpperText = knNumber;
            style.Size = 12.0;
        }

        private static void AddLeaderToDim(LeaderBuilder leaderBuilder, Dimension dim)
        {
            var leader = leaderBuilder.Leaders.CreateLeaderData();
            leader.StubSide = LeaderSide.Inferred;
            leader.Type = LeaderType.Plain;
            leader.Arrowhead = LeaderArrowhead.FilledArrow;
            leader.AddTerminatorAttachment(dim, LeaderAttachmentType.Centered, 0.0, 0.0);
            leaderBuilder.Leaders.Append(leader);
        }

        private static void AddLeaderToPmi(LeaderBuilder leaderBuilder, PmiDimension dim)
        {
            var leader = leaderBuilder.Leaders.CreateLeaderData();
            leader.StubSide = LeaderSide.Inferred;
            leader.Type = LeaderType.Plain;
            leader.Arrowhead = LeaderArrowhead.FilledArrow;
            leader.AddTerminatorAttachment(dim, LeaderAttachmentType.Centered, 0.0, 0.0);
            leaderBuilder.Leaders.Append(leader);
        }

        private static Point3d ComputeAnchor(Dimension dim)
        {
            Point3d origin = dim.AnnotationOrigin;
            return new Point3d(origin.X + LeaderOffsetMm, origin.Y + LeaderOffsetMm, origin.Z);
        }

        private static Point3d ComputePmiAnchor(PmiDimension dim)
        {
            Point3d origin = dim.AnnotationOrigin;
            return new Point3d(origin.X + LeaderOffsetMm, origin.Y + LeaderOffsetMm, origin.Z);
        }
    }
}
