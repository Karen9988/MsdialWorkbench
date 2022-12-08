﻿using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;

namespace CompMs.MsdialCore.Parser
{
    [MessagePack.Union(0, typeof(DataBaseRestorationKey))]
    [MessagePack.Union(1, typeof(MspDbRestorationKey))]
    [MessagePack.Union(2, typeof(TextDbRestorationKey))]
    [MessagePack.Union(3, typeof(StandardRestorationKey))]
    [MessagePack.Union(4, typeof(ShotgunProteomicsRestorationKey))]
    [MessagePack.Union(5, typeof(EadLipidDatabaseRestorationKey))]
    public interface IReferRestorationKey<in TQuery, TReference, TResult, in TDatabase>
    {
        ISerializableAnnotator<TQuery, TReference, TResult, TDatabase> Accept(ILoadAnnotatorVisitor visitor, TDatabase database);
        IAnnotationQueryFactory<MsScanMatchResult> Accept(ICreateAnnotationQueryFactoryVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor, TDatabase database);

        string Key { get; }

        int Priority { get; }
    }
}
