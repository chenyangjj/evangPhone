using EvangSol.Mobibrary.Utilities.Common;

namespace EvangSol.Mobibrary.EvangCustom
{
    public class SingleTapButton : Button
    {
        public event EventHandler? OnAllClickEmbark;
        public event EventHandler? OnAllClickFinish;

        private readonly List<EventHandler> _syncHandlers = new();
        private readonly List<Func<object?, EventArgs, Task>> _asyncHandlers = new();
        private int _pendingOperations = 0;
        private readonly object _lockObject = new object();
        private bool _isBaseHandlerAttached = false;
        private bool _allComplete = false;
        private bool _enableStateChanged = false;

        // Traditional synchronous event
        public new event EventHandler Clicked
        {
            add
            {
                _syncHandlers.Add(value);
                EnsureBaseHandlerAttached();
            }
            remove
            {
                _syncHandlers.Remove(value);
                UpdateBaseHandlerAttachment();
            }
        }

        // New async event
        public event Func<object?, EventArgs, Task> AsyncClicked
        {
            add
            {
                _asyncHandlers.Add(value);
                EnsureBaseHandlerAttached();
            }
            remove
            {
                _asyncHandlers.Remove(value);
                UpdateBaseHandlerAttachment();
            }
        }

        // tracking IsEnabled
        public new bool IsEnabled
        {
            get => base.IsEnabled;
            set
            {
                _enableStateChanged = true;
                Console.WriteLine($"[SingleTapButton] IsEnabled {value} =======================================================================");
                base.IsEnabled = value;
            }
        }

        public int Delay { get; set; } = 500;

        public SingleTapButton()
        {
            // Create and apply a style with visual states
            Style = CreateButtonStyle();
        }

        private Style CreateButtonStyle()
        {
            var style = new Style(typeof(Button));

            // Normal state
            style.Setters.Add(new Setter { Property = BackgroundColorProperty, Value = BaseUtils.GetColor("Primary") });
            style.Setters.Add(new Setter { Property = TextColorProperty, Value = BaseUtils.GetColor("White") });
            style.Setters.Add(new Setter { Property = CornerRadiusProperty, Value = 8 });

            // Visual States
            VisualStateGroupList visualStateGroups = new VisualStateGroupList
            {
                new VisualStateGroup
                {
                    Name = "CommonStates",
                    States =
                    {
                        new VisualState
                        {
                            Name = "Normal"
                        },
                        new VisualState
                        {
                            Name = "Disabled",
                            Setters =
                            {
                                new Setter { Property = BackgroundColorProperty, Value = BaseUtils.GetColor("Gray200") },
                                new Setter { Property = TextColorProperty, Value = BaseUtils.GetColor("Gray950") },
                                //new Setter { Property = OpacityProperty, Value = 0.7 }
                            }
                        }
                    }
                }
            };

            style.Setters.Add(new Setter { Property = VisualStateManager.VisualStateGroupsProperty, Value = visualStateGroups });

            return style;
        }

        private void EnsureBaseHandlerAttached()
        {
            if (!_isBaseHandlerAttached && (_syncHandlers.Count > 0 || _asyncHandlers.Count > 0))
            {
                base.Clicked += OnBaseClicked;
                _isBaseHandlerAttached = true;
            }
        }

        private void UpdateBaseHandlerAttachment()
        {
            if (_isBaseHandlerAttached && _syncHandlers.Count == 0 && _asyncHandlers.Count == 0)
            {
                base.Clicked -= OnBaseClicked;
                _isBaseHandlerAttached = false;
            }
        }

        private async void OnBaseClicked(object? sender, EventArgs e)
        {
            _allComplete = false;
            var tasks = new List<Task>();

            // Register start of operation
            bool isFirstOperation = RegisterOperationStart();

            if (isFirstOperation)
            {
                OnAllOperationsStarting();
            }

            try
            {
                // Execute synchronous handlers (wrapped)
                foreach (var syncHandler in _syncHandlers.ToArray())
                {
                    try
                    {
                        syncHandler(sender, e);
                    }
                    catch (Exception ex)
                    {
                        // Handle sync exceptions
                        System.Diagnostics.Debug.WriteLine($"Sync handler exception: {ex.Message}");
                    }
                }

                // Execute asynchronous handlers
                foreach (var asyncHandler in _asyncHandlers.ToArray())
                {
                    tasks.Add(ExecuteAsyncHandler(asyncHandler, sender, e));
                }

                // add default waiting task
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(Delay);
                    // if IsEnabled has not changed, set it to true
                    if (!_enableStateChanged)
                    {
                        Dispatcher.Dispatch(() => IsEnabled = true);
                        Console.WriteLine($"[SingleTapButton] Waiting Task =======================================================================");
                    }
                }));

                // Wait for all async operations to complete
                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                }
            }
            finally
            {
                // Register completion - this will trigger OnAllOperationsCompleted 
                // when all pending operations are done
                RegisterOperationCompletion();
            }
        }

        private async Task ExecuteAsyncHandler(Func<object?, EventArgs, Task> asyncHandler, object? sender, EventArgs e)
        {
            try
            {
                await asyncHandler(sender, e);
            }
            catch (Exception ex)
            {
                // Handle async exceptions
                System.Diagnostics.Debug.WriteLine($"Async handler exception: {ex.Message}");
            }
        }

        private bool RegisterOperationStart()
        {
            lock (_lockObject)
            {
                _pendingOperations++;
                return _pendingOperations == 1; // First operation
            }
        }

        private bool RegisterOperationCompletion()
        {
            lock (_lockObject)
            {
                _pendingOperations--;
                _allComplete = _pendingOperations == 0;

                if (_allComplete)
                {
                    // Use dispatcher to ensure UI updates happen on main thread
                    Dispatcher.Dispatch(OnAllOperationsCompleted);
                }

                return _allComplete;
            }
        }

        // Called when the first operation starts
        protected virtual void OnAllOperationsStarting()
        {
            IsEnabled = false;
            _enableStateChanged = false;
            Console.WriteLine($"[SingleTapButton] OnAllOperationsStarting =======================================================================");
            OnAllClickEmbark?.Invoke(this, EventArgs.Empty);
        }

        // Called when ALL operations are complete
        protected virtual void OnAllOperationsCompleted()
        {
            Console.WriteLine($"[SingleTapButton] OnAllOperationsCompleted =======================================================================");
            OnAllClickFinish?.Invoke(this, EventArgs.Empty);
        }

        // Helper method to clear all handlers
        public void ClearAllHandlers()
        {
            _syncHandlers.Clear();
            _asyncHandlers.Clear();
            UpdateBaseHandlerAttachment();
        }

        // Property to check if operations are in progress
        public bool IsProcessing => _pendingOperations > 0;

        // Add a handler that automatically gets wrapped based on its signature
        public void AddHandler(Delegate handler)
        {
            switch (handler)
            {
                case EventHandler syncHandler:
                    Clicked += syncHandler;
                    break;
                case Func<object, EventArgs, Task> asyncHandler:
                    AsyncClicked += asyncHandler;
                    break;
                case Action actionHandler:
                    Clicked += (s, e) => actionHandler();
                    break;
                case Func<Task> asyncActionHandler:
                    AsyncClicked += async (s, e) => await asyncActionHandler();
                    break;
                default:
                    throw new ArgumentException("Unsupported handler type");
            }
        }

        // Execute a tap programmatically
        public async Task SimulateTapAsync()
        {
            OnBaseClicked(this, EventArgs.Empty);
            await Task.Delay(1);
        }
    }
}
