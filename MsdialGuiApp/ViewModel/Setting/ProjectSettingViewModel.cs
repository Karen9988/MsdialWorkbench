﻿using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class ProjectSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public ProjectSettingViewModel(ProjectSettingModel model) {
            Model = model;
            SettingViewModels = new ObservableCollection<ISettingViewModel>(
                new ISettingViewModel[]
                {
                    new ProjectParameterSettingViewModel(Model.ProjectParameterSettingModel).AddTo(Disposables),
                });

            ObserveChanges = SettingViewModels.ObserveElementObservableProperty(vm => vm.ObserveChanges).Select(pack => pack.Value);

            ObserveHasErrors = SettingViewModels.ObserveElementObservableProperty(vm => vm.ObserveHasErrors)
                .Switch(_ => SettingViewModels.Select(vm => vm.ObserveHasErrors).CombineLatestValuesAreAnyTrue())
                .Do(v => Console.WriteLine($"Project ObserveHasErrors: {v}"))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ObserveChangeAfterDecision = SettingViewModels.ObserveElementObservableProperty(vm => vm.ObserveChangeAfterDecision)
                .Switch(_ => SettingViewModels.Select(vm => vm.ObserveChangeAfterDecision).CombineLatestValuesAreAnyTrue())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            DatasetSettingViewModel = Model
                .ObserveProperty(m => m.DatasetSettingModel)
                .Select(m => m is null ? null : new DatasetSettingViewModel(m, ObserveChangeAfterDecision.Inverse()))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public ProjectSettingModel Model { get; }

        public ReadOnlyReactivePropertySlim<DatasetSettingViewModel> DatasetSettingViewModel { get; }

        public ObservableCollection<ISettingViewModel> SettingViewModels { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }

        public IObservable<Unit> ObserveChanges { get; }

        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public void Next() {
            foreach (var vm in SettingViewModels) {
                vm.Next();
            }
        }
    }
}
