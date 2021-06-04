﻿using CompMs.MsdialCore.Algorithm.Annotation;

namespace CompMs.MsdialCore.Parser
{
    [MessagePack.Union(0, typeof(DataBaseRestorationKey))]
    [MessagePack.Union(1, typeof(MspDbRestorationKey))]
    [MessagePack.Union(2, typeof(TextDbRestorationKey))]
    public interface IReferRestorationKey
    {
        IMatchResultRefer Accept(IRestorationVisitor visitor);

    }

    [MessagePack.MessagePackObject]
    public abstract class DataBaseRestorationKey : IReferRestorationKey
    {
        public DataBaseRestorationKey(string path) {
            DataBasePath = path;
        }

        [MessagePack.Key(0)]
        public string DataBasePath { get; set; }

        public abstract IMatchResultRefer Accept(IRestorationVisitor visitor);
    }

    [MessagePack.MessagePackObject]
    public class MspDbRestorationKey : DataBaseRestorationKey
    {
        public MspDbRestorationKey(string path) : base(path) {

        }

        public override IMatchResultRefer Accept(IRestorationVisitor visitor) {
            return visitor.Visit(this);
        }
    }

    [MessagePack.MessagePackObject]
    public class TextDbRestorationKey : DataBaseRestorationKey
    {
        public TextDbRestorationKey(string path) : base(path) {

        }

        public override IMatchResultRefer Accept(IRestorationVisitor visitor) {
            return visitor.Visit(this);
        }
    }
}