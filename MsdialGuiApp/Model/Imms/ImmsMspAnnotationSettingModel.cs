﻿using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using System;

namespace CompMs.App.Msdial.Model.Imms
{
    public sealed class ImmsMspAnnotationSettingModel : MspAnnotationSettingModel
    {
        public ImmsMspAnnotationSettingModel(DataBaseAnnotationSettingModelBase other)
            : base(other) {

        }

        protected override ISerializableAnnotatorContainer BuildCore(ParameterBase parameter, MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new ImmsMspAnnotator(molecules, Parameter, parameter.ProjectParam.TargetOmics, AnnotatorID),
                molecules,
                Parameter);
        }
    }
}