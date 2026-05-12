using System;
using NXOpen;
using NXOpen.Annotations;

namespace KnBalloonTool.Services
{
    public static class BalloonService
    {
        private const double LeaderOffsetMm = 15.0;
        private const double SymbolSize = 12.0;

        public static IdSymbol CreateDraftingBalloon(Part part, Dimension dim, string knNumber)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            if (dim == null) throw new ArgumentNullException(nameof(dim));
            if (string.IsNullOrEmpty(knNumber)) throw new ArgumentException("knNumber required");

            IdSymbolBuilder builder = part.Annotations.IdSymbols.CreateIdSymbolBuilder(null);
            try
            {
                builder.Type = IdSymbolBuilder.SymbolTypes.Circle;
                builder.UpperText = knNumber;
                builder.Size = SymbolSize;

                Point3d anchor = OffsetPoint(dim.AnnotationOrigin);
                builder.Origin.Origin.SetValue(null, null, anchor);

                AttachLeader(part, builder.Leader, dim);

                var created = (IdSymbol)builder.Commit();
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

            PmiIdSymbolBuilder builder = part.PmiManager.PmiIdSymbols.CreatePmiIdSymbolBuilder(null);
            try
            {
                builder.Type = IdSymbolBuilder.SymbolTypes.Circle;
                builder.UpperText = knNumber;
                builder.Size = SymbolSize;

                Point3d anchor = OffsetPoint(dim.AnnotationOrigin);
                builder.Origin.Origin.SetValue(null, null, anchor);

                AttachLeader(part, builder.Leader, dim);

                var created = (PmiIdSymbol)builder.Commit();
                MappingService.MarkBalloonAsToolOwned(created, knNumber, dim);
                return created;
            }
            finally
            {
                builder.Destroy();
            }
        }

        private static void AttachLeader(Part part, LeaderBuilder leaderBuilder, NXObject target)
        {
            LeaderData leader = part.Annotations.CreateLeaderData();
            leader.StubSide = LeaderSide.Inferred;
            leader.Type = LeaderType.Plain;
            leader.Arrowhead = LeaderData.ArrowheadType.FilledArrow;
            leader.VerticalAttachment = LeaderVerticalAttachment.Center;
            leader.SetTermObject(target);

            leaderBuilder.Leaders.Append(leader);
        }

        private static Point3d OffsetPoint(Point3d origin)
        {
            return new Point3d(origin.X + LeaderOffsetMm, origin.Y + LeaderOffsetMm, origin.Z);
        }
    }
}
