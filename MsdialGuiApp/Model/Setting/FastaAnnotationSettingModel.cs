﻿using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting {
    abstract class FastaAnnotationSettingModel : BindableBase, IAnnotationSettingModel {
        public FastaAnnotationSettingModel() {

        }

        public FastaAnnotationSettingModel(FastaAnnotationSettingModel model) {
            DataBasePath = model.DataBasePath;
            DataBaseID = model.DataBaseID;
            DBSource = model.DBSource;
            AnnotationSource = model.AnnotationSource;
            AnnotatorID = model.AnnotatorID;
            MsRefSearchParameter = model.MsRefSearchParameter;
            ProteomicsParameter = model.ProteomicsParameter;
        }

        public string DataBasePath {
            get => dataBasePath;
            set => SetProperty(ref dataBasePath, value);
        }
        private string dataBasePath = string.Empty;

        public string DataBaseID {
            get => dataBaseID;
            set => SetProperty(ref dataBaseID, value);
        }
        private string dataBaseID = string.Empty;

        public DataBaseSource DBSource {
            get => source;
            set => SetProperty(ref source, value);
        }
        private DataBaseSource source = DataBaseSource.None;

        public SourceType AnnotationSource {
            get => annotationSource;
            set => SetProperty(ref annotationSource, value);
        }
        private SourceType annotationSource;

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID;

        public MsRefSearchParameterBase MsRefSearchParameter {
            get => msRefSearchParameter;
            set => SetProperty(ref msRefSearchParameter, value);
        }
        private MsRefSearchParameterBase msRefSearchParameter = new MsRefSearchParameterBase();

        public ProteomicsParameter ProteomicsParameter {
            get => proteomicsParameter;
            set => SetProperty(ref proteomicsParameter, value);
        }
        private ProteomicsParameter proteomicsParameter = new ProteomicsParameter();


        public abstract ISerializableAnnotatorContainer Build(ParameterBase parameter);

        protected static ShotgunProteomicsDB LoadShotgunProteomicsDB(string path, string id, ParameterBase parameter) {
            return new ShotgunProteomicsDB(path, id, parameter.ProteomicsParam, parameter.MspSearchParam);
        }
    }
}
