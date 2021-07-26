﻿using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;

namespace CompMs.App.Msdial.Model.Core
{

    internal abstract class MethodModelBase : BindableBase, IDisposable
    {
        public MethodModelBase(
            IEnumerable<AnalysisFileBean> analysisFiles,
            IEnumerable<AlignmentFileBean> alignmentFiles) {
            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(analysisFiles ?? new AnalysisFileBean[] { });
            AlignmentFiles = new ObservableCollection<AlignmentFileBean>(alignmentFiles ?? new AlignmentFileBean[] { });
        }

        public AnalysisFileBean AnalysisFile {
            get => analysisFile;
            set => SetProperty(ref analysisFile, value);
        }
        private AnalysisFileBean analysisFile;

        public ObservableCollection<AnalysisFileBean> AnalysisFiles { get; }

        public void LoadAnalysisFile(AnalysisFileBean analysisFile) {
            if (AnalysisFile == analysisFile || analysisFile is null) {
                return;
            }
            AnalysisFile = analysisFile;
            LoadAnalysisFileCore(AnalysisFile);
        }

        protected abstract void LoadAnalysisFileCore(AnalysisFileBean analysisFile);

        public AlignmentFileBean AlignmentFile {
            get => alignmentFile;
            set => SetProperty(ref alignmentFile, value);
        }
        private AlignmentFileBean alignmentFile;

        public ObservableCollection<AlignmentFileBean> AlignmentFiles { get; }

        public void LoadAlignmentFile(AlignmentFileBean alignmentFile) {
            if (AlignmentFile == alignmentFile || alignmentFile is null) {
                return;
            }
            AlignmentFile = alignmentFile;
            LoadAlignmentFileCore(AlignmentFile);
        }

        protected abstract void LoadAlignmentFileCore(AlignmentFileBean alignmentFile);

        private bool disposedValue;
        protected CompositeDisposable Disposables = new CompositeDisposable();

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Disposables.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}