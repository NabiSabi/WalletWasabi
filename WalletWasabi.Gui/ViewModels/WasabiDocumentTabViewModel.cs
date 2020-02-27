using Avalonia.Threading;
using AvalonStudio.Documents;
using AvalonStudio.Extensibility;
using AvalonStudio.MVVM;
using AvalonStudio.Shell;
using Dock.Model;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;

namespace WalletWasabi.Gui.ViewModels
{
	public abstract class WasabiDocumentTabViewModel : ViewModelBase, IDocumentTabViewModel
	{
		private string _title;
		private bool _isSelected;
		private bool _isClosed;
		private object _dialogResult;

		protected WasabiDocumentTabViewModel(string title)
		{
			Title = title;
			DoItCommand = ReactiveCommand.Create(DisplayActionTab);

			DoItCommand.ThrownExceptions
				.ObserveOn(RxApp.TaskpoolScheduler)
				.Subscribe(ex => Logger.LogError(ex));
		}

		private CompositeDisposable Disposables { get; set; }

		public object Context { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		public IDockable Parent { get; set; }

		public string Title
		{
			get => _title;
			set => this.RaiseAndSetIfChanged(ref _title, value);
		}

		public bool IsSelected
		{
			get => _isSelected;
			set => this.RaiseAndSetIfChanged(ref _isSelected, value);
		}

		public bool IsClosed
		{
			get => _isClosed;
			set => this.RaiseAndSetIfChanged(ref _isClosed, value);
		}

		public object DialogResult
		{
			get => _dialogResult;
			set => this.RaiseAndSetIfChanged(ref _dialogResult, value);
		}

		public bool IsDirty { get; set; }

		public virtual void OnSelected()
		{
			IsSelected = true;
		}

		public virtual void OnDeselected()
		{
			IsSelected = false;
		}

		void IDockableViewModel.OnOpen()
		{
			Disposables = Disposables is null ? new CompositeDisposable() : throw new NotSupportedException($"Cannot open {GetType().Name} before closing it.");

			OnOpen(Disposables);
		}

		public virtual void OnOpen(CompositeDisposable disposables)
		{			
			IsClosed = false;
		}

		public virtual bool OnClose()
		{
			Disposables.Dispose();
			Disposables = null;

			IsSelected = false;
			IoC.Get<IShell>().RemoveDocument(this);
			IsClosed = true;
			return true;
		}

		public void DisplayActionTab()
		{
			IoC.Get<IShell>().AddOrSelectDocument(this);
		}

		public ReactiveCommand<Unit, Unit> DoItCommand { get; }
		
		public void Select()
		{
			IoC.Get<IShell>().Select(this);
		}

		public async Task<object> ShowDialogAsync()
		{
			DialogResult = null;
			DisplayActionTab();

			while (!IsClosed)
			{
				if (!IsSelected) // Prevent de-selection of tab.
				{
					Select();
				}
				await Task.Delay(100);
			}
			return DialogResult;
		}
	}
}
