using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace NumericUpDown
{
    /// <summary>
    /// Determins whether the value bar behind the value text should be visible
    /// </summary>
    public enum NumericUpDownValueBarVisibility
    {
        /// <summary>
        /// Visible
        /// </summary>
        Visible,
        /// <summary>
        /// Collapsed
        /// </summary>
        Collapsed
    }

    /// <summary>
    /// NumericUpDown control - for representing values that can be
    /// entered with keyboard,
    /// using increment/decrement buttons
    /// as well as swiping over the control.
    /// </summary>
    [TemplatePart(Name = ValueTextBoxName, Type = typeof(TextBox))]
    [TemplatePart(Name = ValueBarName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = DragOverlayName, Type = typeof(UIElement))]
    [TemplatePart(Name = DecrementButtonName, Type = typeof(RepeatButton))]
    [TemplatePart(Name = IncrementButtonName, Type = typeof(RepeatButton))]
    [TemplateVisualState(GroupName = "IncrementalButtonStates", Name = "IncrementEnabled")]
    [TemplateVisualState(GroupName = "IncrementalButtonStates", Name = "IncrementDisabled")]
    [TemplateVisualState(GroupName = "DecrementalButtonStates", Name = "DecrementEnabled")]
    [TemplateVisualState(GroupName = "DecrementalButtonStates", Name = "DecrementDisabled")]
    public sealed class NumericUpDown : RangeBase
    {
        private const string DecrementButtonName = "PART_DecrementButton";
        private const string IncrementButtonName = "PART_IncrementButton";
        private const string DragOverlayName = "PART_DragOverlay";
        private const string ValueTextBoxName = "PART_ValueTextBox";
        private const string ValueBarName = "PART_ValueBar";
        private UIElement _dragOverlay;
        private UpDownTextBox _valueTextBox;
        private RepeatButton _decrementButton;
        private RepeatButton _incrementButton;
        private FrameworkElement _valueBar;
        private bool _isDragUpdated;
        private bool _isChangingTextWithCode;
        private bool _isChangingValueWithCode;
        private double _unusedManipulationDelta;

        private bool _isDraggingWithMouse;
        private MouseDevice _mouseDevice;
        private const double MinMouseDragDelta = 2;
        private double _totalDeltaX;
        private double _totalDeltaY;

        #region ValueFormat
        /// <summary>
        /// ValueFormat Dependency Property
        /// </summary>
        public static readonly DependencyProperty ValueFormatProperty =
            DependencyProperty.Register(
                "ValueFormat",
                typeof(string),
                typeof(NumericUpDown),
                new PropertyMetadata("F2", OnValueFormatChanged));

        /// <summary>
        /// Gets or sets the ValueFormat property. This dependency property 
        /// indicates the format of the value string.
        /// </summary>
        public string ValueFormat
        {
            get { return (string)this.GetValue(ValueFormatProperty); }
            set { this.SetValue(ValueFormatProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ValueFormat property.
        /// </summary>
        /// <param name="d">
        /// The <see cref="DependencyObject"/> on which
        /// the property has changed value.
        /// </param>
        /// <param name="e">
        /// Event data that is issued by any event that
        /// tracks changes to the effective value of this property.
        /// </param>
        private static void OnValueFormatChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (NumericUpDown)d;
            target.OnValueFormatChanged();
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes
        /// to the ValueFormat property.
        /// </summary>
        private void OnValueFormatChanged()
        {
            this.UpdateValueText();
        }
        #endregion

        #region ValueBarVisibility
        /// <summary>
        /// ValueBarVisibility Dependency Property
        /// </summary>
        public static readonly DependencyProperty ValueBarVisibilityProperty =
            DependencyProperty.Register(
                "ValueBarVisibility",
                typeof(NumericUpDownValueBarVisibility),
                typeof(NumericUpDown),
                new PropertyMetadata(NumericUpDownValueBarVisibility.Visible, OnValueBarVisibilityChanged));

        /// <summary>
        /// Gets or sets the ValueBarVisibility property. This dependency property 
        /// indicates whether the value bar should be shown.
        /// </summary>
        public NumericUpDownValueBarVisibility ValueBarVisibility
        {
            get { return (NumericUpDownValueBarVisibility)this.GetValue(ValueBarVisibilityProperty); }
            set { this.SetValue(ValueBarVisibilityProperty, value); }
        }

        /// <summary>
        /// Handles changes to the ValueBarVisibility property.
        /// </summary>
        /// <param name="d">
        /// The <see cref="DependencyObject"/> on which
        /// the property has changed value.
        /// </param>
        /// <param name="e">
        /// Event data that is issued by any event that
        /// tracks changes to the effective value of this property.
        /// </param>
        private static void OnValueBarVisibilityChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (NumericUpDown)d;
            target.OnValueBarVisibilityChanged();
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes
        /// to the ValueBarVisibility property.
        /// </summary>
        private void OnValueBarVisibilityChanged()
        {
            this.UpdateValueBar();
        }
        #endregion

        #region IsReadOnly
        /// <summary>
        /// IsReadOnly Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                "IsReadOnly",
                typeof(bool),
                typeof(NumericUpDown),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        /// <summary>
        /// Gets or sets the IsReadOnly property. This dependency property 
        /// indicates whether the box should only allow to read the values by copying and pasting them.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)this.GetValue(IsReadOnlyProperty); }
            set { this.SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// Handles changes to the IsReadOnly property.
        /// </summary>
        /// <param name="d">
        /// The <see cref="DependencyObject"/> on which
        /// the property has changed value.
        /// </param>
        /// <param name="e">
        /// Event data that is issued by any event that
        /// tracks changes to the effective value of this property.
        /// </param>
        private static void OnIsReadOnlyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (NumericUpDown)d;
            target.OnIsReadOnlyChanged();
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes
        /// to the IsReadOnly property.
        /// </summary>
        private void OnIsReadOnlyChanged()
        {
            this.UpdateIsReadOnlyDependants();
        }
        #endregion

        #region DragSpeed
        /// <summary>
        /// DragSpeed Dependency Property
        /// </summary>
        public static readonly DependencyProperty DragSpeedProperty =
            DependencyProperty.Register(
                "DragSpeed",
                typeof(double),
                typeof(NumericUpDown),
                new PropertyMetadata(double.NaN));

        /// <summary>
        /// Gets or sets the DragSpeed property. This dependency property 
        /// indicates the speed with which the value changes when manipulated with dragging.
        /// The default value of double.NaN indicates that the value will change by (Maximum - Minimum),
        /// when the control is dragged a full screen length.
        /// </summary>
        public double DragSpeed
        {
            get { return (double)this.GetValue(DragSpeedProperty); }
            set { this.SetValue(DragSpeedProperty, value); }
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericUpDown" /> class.
        /// </summary>
        public NumericUpDown()
        {
            this.DefaultStyleKey = typeof(NumericUpDown);
            this.Loaded += this.OnLoaded;
            this.Unloaded += this.OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_dragOverlay != null)
            {
                Window.Current.CoreWindow.PointerReleased += this.CoreWindowOnPointerReleased;
                Window.Current.CoreWindow.VisibilityChanged += this.OnCoreWindowVisibilityChanged;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerReleased -= this.CoreWindowOnPointerReleased;
            Window.Current.CoreWindow.VisibilityChanged -= this.OnCoreWindowVisibilityChanged;
        }

        #region OnApplyTemplate()
        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuilding layout pass) call ApplyTemplate. In simplest terms, this means the method is called just before a UI element displays in your app. Override this method to influence the default post-template logic of a class.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (DesignMode.DesignModeEnabled)
            {
                return;
            }

            this.GotFocus += this.OnGotFocus;
            this.LostFocus += this.OnLostFocus;
            this.PointerWheelChanged += this.OnPointerWheelChanged;
            _valueTextBox = this.GetTemplateChild(ValueTextBoxName) as UpDownTextBox;
            _dragOverlay = this.GetTemplateChild(DragOverlayName) as UIElement;
            _decrementButton = this.GetTemplateChild(DecrementButtonName) as RepeatButton;
            _incrementButton = this.GetTemplateChild(IncrementButtonName) as RepeatButton;
            _valueBar = this.GetTemplateChild(ValueBarName) as FrameworkElement;

            if (_valueTextBox != null)
            {
                _valueTextBox.LostFocus += this.OnValueTextBoxLostFocus;
                _valueTextBox.GotFocus += this.OnValueTextBoxGotFocus;
                _valueTextBox.Text = this.Value.ToString(CultureInfo.CurrentCulture);
                _valueTextBox.TextChanged += this.OnValueTextBoxTextChanged;
                _valueTextBox.KeyDown += this.OnValueTextBoxKeyDown;
                _valueTextBox.UpPressed += (s, e) => this.Increment();
                _valueTextBox.DownPressed += (s, e) => this.Decrement();
                _valueTextBox.PointerExited += this.OnValueTextBoxPointerExited;
            }

            if (_dragOverlay != null)
            {
                _dragOverlay.Tapped += this.OnDragOverlayTapped;
                _dragOverlay.ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY;
                _dragOverlay.PointerPressed += this.OnDragOverlayPointerPressed;
                _dragOverlay.PointerReleased += this.OnDragOverlayPointerReleased;
                _dragOverlay.PointerCaptureLost += this.OnDragOverlayPointerCaptureLost;
            }

            if (_decrementButton != null)
            {
                _decrementButton.Click += this.OnDecrementButtonClick;
                var pcc =
                    new PropertyChangeEventSource<bool>
                        (_decrementButton, "IsPressed");
                pcc.ValueChanged += this.OnDecrementButtonIsPressedChanged;
            }

            if (_incrementButton != null)
            {
                _incrementButton.Click += this.OnIncrementButtonClick;
                var pcc =
                    new PropertyChangeEventSource<bool>
                        (_incrementButton, "IsPressed");
                pcc.ValueChanged += this.OnIncrementButtonIsPressedChanged;
            }

            if (_valueBar != null)
            {
                _valueBar.SizeChanged += this.OnValueBarSizeChanged;

                this.UpdateValueBar();
            }

            this.UpdateIsReadOnlyDependants();
            this.SetValidIncrementDirection();
        }

        private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            if (!_hasFocus)
            {
                return;
            }

            var delta = pointerRoutedEventArgs.GetCurrentPoint(this).Properties.MouseWheelDelta;

            if (delta < 0)
            {
                this.Decrement();
            }
            else
            {
                this.Increment();
            }

            pointerRoutedEventArgs.Handled = true;
        }

        private bool _hasFocus;
        private const double Epsilon = .00001;

        private void OnLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            _hasFocus = false;
        }

        private void OnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            _hasFocus = true;
        }

        private void OnValueTextBoxTextChanged(object sender, TextChangedEventArgs textChangedEventArgs)
        {
            this.UpdateValueFromText();
        }

        private void OnValueTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                if (this.UpdateValueFromText())
                {
                    this.UpdateValueText();
                    _valueTextBox.SelectAll();
                    e.Handled = true;
                }
            }
        }

        private bool UpdateValueFromText()
        {
            if (_isChangingTextWithCode)
            {
                return false;
            }

            double val;

            if (double.TryParse(_valueTextBox.Text, NumberStyles.Any, CultureInfo.CurrentUICulture, out val) ||
                Calculator.TryCalculate(_valueTextBox.Text, out val))
            {
                _isChangingValueWithCode = true;
                this.SetValueAndUpdateValidDirections(val);
                _isChangingValueWithCode = false;

                return true;
            }

            return false;
        }

        #endregion

        #region Button event handlers
        private void OnDecrementButtonIsPressedChanged(object decrementButton, bool isPressed)
        {
            // TODO: The thinking was to handle speed and acceleration of value changes manually on a regular Button when it is pressed.
            // Currently just using RepeatButtons
        }

        private void OnIncrementButtonIsPressedChanged(object incrementButton, bool isPressed)
        {
        }

        private void OnDecrementButtonClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (Window.Current.CoreWindow.IsInputEnabled)
            {
                this.Decrement();
            }
        }

        private void OnIncrementButtonClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (Window.Current.CoreWindow.IsInputEnabled)
            {
                this.Increment();
            }
        }
        #endregion

        /// <summary>
        /// Decrements the value by SmallChange.
        /// </summary>
        /// <returns><c>true</c> if the value was decremented by exactly <c>SmallChange</c>; <c>false</c> if it was constrained.</returns>
        public bool Decrement()
        {
            return this.SetValueAndUpdateValidDirections(this.Value - this.SmallChange);
        }

        public bool Increment()
        {
            return this.SetValueAndUpdateValidDirections(this.Value + this.SmallChange);
        }

        private void OnValueTextBoxGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            if (_dragOverlay != null)
            {
                _dragOverlay.IsHitTestVisible = false;
            }

            _valueTextBox.SelectAll();
        }

        private void OnValueTextBoxLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            if (_dragOverlay != null)
            {
                _dragOverlay.IsHitTestVisible = true;
                this.UpdateValueText();
            }
        }

        private void OnDragOverlayPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _dragOverlay.CapturePointer(e.Pointer);

            _totalDeltaX = 0;
            _totalDeltaY = 0;

            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
            {
                _isDraggingWithMouse = true;
                _mouseDevice = MouseDevice.GetForCurrentView();
                _mouseDevice.MouseMoved += this.OnMouseDragged;
                Window.Current.CoreWindow.PointerCursor = null;
            }
            else
            {
                _dragOverlay.ManipulationDelta += this.OnDragOverlayManipulationDelta;
            }
        }

        private void CoreWindowOnPointerReleased(CoreWindow sender, PointerEventArgs args)
        {
            if (_isDragUpdated)
            {
                args.Handled = true;
                this.ResumeValueTextBoxTabStopAsync();
            }
        }

        private void SuspendValueTextBoxTabStop()
        {
            if (_valueTextBox != null)
            {
                _valueTextBox.IsTabStop = false;
            }
        }

        private async void ResumeValueTextBoxTabStopAsync()
        {
            // We need to wait for just a bit to allow manipulation events to complete.
            // It's a bit hacky, but it's the simplest solution.
            await Task.Delay(100);

            if (_valueTextBox != null)
            {
                _valueTextBox.IsTabStop = true;
            }
        }

        private void OnDragOverlayPointerReleased(object sender, PointerRoutedEventArgs args)
        {
            this.EndDragging(args);
        }

        private void OnDragOverlayPointerCaptureLost(object sender, PointerRoutedEventArgs args)
        {
            this.EndDragging(args);
        }

        private void EndDragging(PointerRoutedEventArgs args)
        {
            if (_isDraggingWithMouse)
            {
                _isDraggingWithMouse = false;
                _mouseDevice.MouseMoved -= this.OnMouseDragged;
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.SizeAll, 1);
                _mouseDevice = null;
            }
            else if (_dragOverlay != null)
            {
                _dragOverlay.ManipulationDelta -= this.OnDragOverlayManipulationDelta;
            }

            if (_isDragUpdated)
            {
                if (args != null)
                {
                    args.Handled = true;
                }

                this.ResumeValueTextBoxTabStopAsync();
            }
        }

        private void OnCoreWindowVisibilityChanged(CoreWindow sender, VisibilityChangedEventArgs args)
        {
            // There are cases where pointer isn't getting released - this should hopefully end dragging too.
            if (!args.Visible)
            {
#pragma warning disable 4014
                this.EndDragging(null);
#pragma warning restore 4014
            }
        }

        private void OnMouseDragged(MouseDevice sender, MouseEventArgs args)
        {
            var dx = args.MouseDelta.X;
            var dy = args.MouseDelta.Y;

            if (dx > 200 || dx < -200 || dy > 200 || dy < -200)
            {
                return;
            }

            _totalDeltaX += dx;
            _totalDeltaY += dy;

            if (_totalDeltaX > MinMouseDragDelta ||
                _totalDeltaX < -MinMouseDragDelta ||
                _totalDeltaY > MinMouseDragDelta ||
                _totalDeltaY < -MinMouseDragDelta)
            {
                this.UpdateByDragging(_totalDeltaX, _totalDeltaY);
                _totalDeltaX = 0;
                _totalDeltaY = 0;
            }
        }

        private void OnDragOverlayManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs manipulationDeltaRoutedEventArgs)
        {
            var dx = manipulationDeltaRoutedEventArgs.Delta.Translation.X;
            var dy = manipulationDeltaRoutedEventArgs.Delta.Translation.Y;

            if (this.UpdateByDragging(dx, dy))
                return;

            manipulationDeltaRoutedEventArgs.Handled = true;
        }

        private bool UpdateByDragging(double dx, double dy)
        {
            if (!this.IsEnabled ||
                this.IsReadOnly ||
                // ReSharper disable CompareOfFloatsByEqualityOperator
                dx == 0 && dy == 0)
            // ReSharper restore CompareOfFloatsByEqualityOperator
            {
                return false;
            }

            double delta;

            if (Math.Abs(dx) > Math.Abs(dy))
            {
                delta = dx;
            }
            else
            {
                delta = -dy;
            }

            this.ApplyManipulationDelta(delta);

            this.SuspendValueTextBoxTabStop();

            _isDragUpdated = true;

            return true;
        }

        private void ApplyManipulationDelta(double delta)
        {
            if (Math.Sign(delta) == Math.Sign(_unusedManipulationDelta))
                _unusedManipulationDelta += delta;
            else
                _unusedManipulationDelta = delta;

            if (_unusedManipulationDelta <= 0 && this.Value == this.Minimum)
            {
                _unusedManipulationDelta = 0;
                return;
            }

            if (_unusedManipulationDelta >= 0 && this.Value == this.Maximum)
            {
                _unusedManipulationDelta = 0;
                return;
            }

            double smallerScreenDimension;

            if (Window.Current != null)
            {
                smallerScreenDimension = Math.Min(Window.Current.Bounds.Width, Window.Current.Bounds.Height);
            }
            else
            {
                smallerScreenDimension = 768;
            }

            var speed = this.DragSpeed;

            if (double.IsNaN(speed) ||
                double.IsInfinity(speed))
            {
                speed = this.Maximum - this.Minimum;
            }

            if (double.IsNaN(speed) ||
                double.IsInfinity(speed))
            {
                speed = double.MaxValue;
            }

            var screenAdjustedDelta = speed * _unusedManipulationDelta / smallerScreenDimension;
            this.SetValueAndUpdateValidDirections(this.Value + screenAdjustedDelta);
            _unusedManipulationDelta = 0;
        }

        private void OnDragOverlayTapped(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
        {
            if (this.IsEnabled &&
                _valueTextBox != null &&
                _valueTextBox.IsTabStop)
            {
                _valueTextBox.Focus(FocusState.Programmatic);
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.IBeam, 0);
            }
        }
        private void OnValueTextBoxPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.PointerCursor.Type == CoreCursorType.IBeam)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }
        }

        /// <summary>
        /// Fires the ValueChanged routed event.
        /// </summary>
        /// <param name="oldValue">Old value of the Value property.</param>
        /// <param name="newValue">New value of the Value property.</param>
        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);

            this.UpdateValueBar();

            if (!_isChangingValueWithCode)
            {
                this.UpdateValueText();
            }
        }

        private void UpdateValueBar()
        {
            if (_valueBar == null)
                return;

            var effectiveValueBarVisibility = this.ValueBarVisibility;

            if (effectiveValueBarVisibility == NumericUpDownValueBarVisibility.Collapsed)
            {
                _valueBar.Visibility = Visibility.Collapsed;

                return;
            }

            _valueBar.Clip =
                new RectangleGeometry
                {
                    Rect = new Rect
                    {
                        X = 0,
                        Y = 0,
                        Height = _valueBar.ActualHeight,
                        Width = _valueBar.ActualWidth * (this.Value - this.Minimum) / (this.Maximum - this.Minimum)
                    }
                };

            //_valueBar.Width =
            //    _valueTextBox.ActualWidth * (Value - Minimum) / (Maximum - Minimum);
        }

        private void OnValueBarSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            this.UpdateValueBar();
        }

        private void UpdateValueText()
        {
            if (_valueTextBox != null)
            {
                _isChangingTextWithCode = true;
                _valueTextBox.Text = this.Value.ToString(this.ValueFormat);
                _isChangingTextWithCode = false;
            }
        }

        private void UpdateIsReadOnlyDependants()
        {
            if (_decrementButton != null)
            {
                _decrementButton.Visibility = this.IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
            }

            if (_incrementButton != null)
            {
                _incrementButton.Visibility = this.IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private bool SetValueAndUpdateValidDirections(double value)
        {
            // Range coercion is handled by base class.
            var oldValue = this.Value;
            this.Value = value;
            this.SetValidIncrementDirection();

            return Math.Abs(this.Value - oldValue) > Epsilon;
        }

        private void SetValidIncrementDirection()
        {
            VisualStateManager.GoToState(this, this.Value < this.Maximum ? "IncrementEnabled" : "IncrementDisabled", true);
            VisualStateManager.GoToState(this, this.Value > this.Minimum ? "DecrementEnabled" : "DecrementDisabled", true);
        }


        public class PropertyChangeEventSource<TPropertyType>
        : FrameworkElement
        {
            /// <summary>
            /// Occurs when the value changes.
            /// </summary>
            public event EventHandler<TPropertyType> ValueChanged;
            private readonly DependencyObject _source;

            #region Value
            /// <summary>
            /// Value Dependency Property
            /// </summary>
            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register(
                    "Value",
                    typeof(TPropertyType),
                    typeof(PropertyChangeEventSource<TPropertyType>),
                    new PropertyMetadata(default(TPropertyType), OnValueChanged));

            /// <summary>
            /// Gets or sets the Value property. This dependency property 
            /// indicates the value.
            /// </summary>
            public TPropertyType Value
            {
                get { return (TPropertyType)GetValue(ValueProperty); }
                set { SetValue(ValueProperty, value); }
            }

            /// <summary>
            /// Handles changes to the Value property.
            /// </summary>
            /// <param name="d">
            /// The <see cref="DependencyObject"/> on which
            /// the property has changed value.
            /// </param>
            /// <param name="e">
            /// Event data that is issued by any event that
            /// tracks changes to the effective value of this property.
            /// </param>
            private static void OnValueChanged(
                DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var target = (PropertyChangeEventSource<TPropertyType>)d;
                TPropertyType oldValue = (TPropertyType)e.OldValue;
                TPropertyType newValue = target.Value;
                target.OnValueChanged(oldValue, newValue);
            }

            /// <summary>
            /// Provides derived classes an opportunity to handle changes
            /// to the Value property.
            /// </summary>
            /// <param name="oldValue">The old Value value</param>
            /// <param name="newValue">The new Value value</param>
            private void OnValueChanged(
                TPropertyType oldValue, TPropertyType newValue)
            {
                var handler = ValueChanged;

                if (handler != null)
                {
                    handler(_source, newValue);
                }
            }
            #endregion

            #region CTOR
            /// <summary>
            /// Initializes a new instance of the <see cref="PropertyChangeEventSource{TPropertyType}"/> class.
            /// </summary>
            /// <param name="source">The source.</param>
            /// <param name="propertyName">Name of the property.</param>
            /// <param name="bindingMode">The binding mode.</param>
            public PropertyChangeEventSource(
                DependencyObject source,
                string propertyName,
                BindingMode bindingMode = BindingMode.TwoWay)
            {
                //var panel =
                //    ((DependencyObject)Window.Current.Content).GetFirstDescendantOfType<Panel>();
                //panel.Children.Add(this);
                _source = source;

                // Bind to the property to be able to get its changes relayed as events throug the ValueChanged event.
                var binding =
                    new Binding
                    {
                        Source = source,
                        Path = new PropertyPath(propertyName),
                        Mode = bindingMode
                    };

                this.SetBinding(
                    ValueProperty,
                    binding);
            }
            #endregion
        }
    }

    public static class Calculator
    {
        private static Func<double, double, double> AddFunc = (d0, d1) => d0 + d1;
        private static Func<double, double, double> SubtractFunc = (d0, d1) => d0 - d1;
        private static Func<double, double, double> MultiplyFunc = (d0, d1) => d0 * d1;
        private static Func<double, double, double> DivideFunc = (d0, d1) => d0 / d1;
        private static Func<double, double, double> ModuloFunc = (d0, d1) => d0 % d1;
        private static Func<double, double, double> PowerFunc = (d0, d1) => Math.Pow(d0, d1);
        private static Func<double, double, double> NumberFunc = (d0, d1) => d0;

        private static bool IsLeftFirst(
            this Func<double, double, double> f1,
            Func<double, double, double> f2)
        {
            if ((f1 == AddFunc || f1 == SubtractFunc) &&
                (f2 != AddFunc && f2 != SubtractFunc) ||
                (f1 == MultiplyFunc || f1 == DivideFunc) && f2 == PowerFunc)
            {
                return false;
            }

            return true;
        }

        private struct Operation
        {
            private readonly double leftValue;
            public readonly Func<double, double, double> Function;
            public readonly int Parentheses;

            public double GetResult(double rightValue)
            {
                return Function(leftValue, rightValue);
            }

            public Operation(double leftValue, Func<double, double, double> function, int parentheses)
            {
                this.leftValue = leftValue;
                this.Function = function;
                this.Parentheses = parentheses;
            }
        }

        /// <summary>
        /// Parses an arithmetic problem
        /// </summary>
        /// <remarks>
        /// Supported symbols are +-*/%^().
        /// </remarks>
        /// <param name="expression">The string that specifies the expression.</param>
        /// <returns>Result of the calculation.</returns>
        public static double Calculate(string expression)
        {
            double result;

            TryCalculate(expression, out result, true);

            // Note - if TryCalculate failed - it would throw an exception, so we don't need to check the result.

            return result;
        }

        /// <summary>
        /// Parses an arithmetic problem
        /// </summary>
        /// <remarks>
        /// Supported symbols are +-*/%^().
        /// No error checking currently other than what double.TryParse() used internally does.
        /// </remarks>
        /// <param name="expression">The string that specifies the expression.</param>
        /// <param name="result">The result.</param>
        /// <returns>True if calculation/parsing succeeded.</returns>
        public static bool TryCalculate(string expression, out double result)
        {
            return TryCalculate(expression, out result, false);
        }

        /// <summary>
        /// Parses an arithmetic problem
        /// </summary>
        /// <remarks>
        /// Supported symbols are +-*/%^().
        /// </remarks>
        /// <param name="expression">The string that specifies the expression.</param>
        /// <param name="result">The result.</param>
        /// <param name="throwOnError">If true and syntex error is found - throws exception. Otherwise - method returns false and result is NaN.</param>
        /// <returns>True if calculation/parsing succeeded.</returns>
        private static bool TryCalculate(string expression, out double result, bool throwOnError)
        {
            if (expression == null)
            {
                if (throwOnError)
                {
                    throw new ArgumentNullException(expression);
                }

                result = double.NaN;
                return false;
            }

            expression = expression.Replace(" ", "");
            var stack = new Stack<Operation>();

            int start = 0;
            int length = expression.Length;

            if (length == 0)
            {
                if (throwOnError)
                {
                    throw new ArgumentException("Empty expression can't be calculated.");
                }

                result = double.NaN;
                return false;
            }

            int parentheses = 0;
            bool valueStarted = false;

            // only support single character currency symbols, separators etc. for now
            char currencySymbol = CultureInfo.CurrentUICulture.NumberFormat.CurrencySymbol.Length == 1 ? CultureInfo.CurrentUICulture.NumberFormat.CurrencySymbol[0] : '$';
            char decimalSeparator = CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator.Length == 1 ? CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator[0] : '.';
            char groupSeparator = CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSeparator.Length == 1 ? CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSeparator[0] : '\'';

            for (int i = 0; i < length; i++)
            {
                var c = expression[i];

                // Number
                if (c >= '0' && c <= '9' || // digit
                    c == 'E' ||
                    c == 'e' ||
                    c == decimalSeparator ||
                    c == groupSeparator ||
                    c == currencySymbol ||
                    c == '-' && i == start && (i + 1 < length && expression[i + 1] >= '0' && expression[i + 1] <= '9') // leading negation
                    )
                {
                    if (c == 'E' || c == 'e')
                    {
                        valueStarted = true;

                        if (i + 1 == length)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException(string.Format("{0} at position {1} is not an expected character at end of {2}", c, i, expression));
                            }

                            result = double.NaN;
                            return false;
                        }

                        //i++;

                        //c = expression[i];

                        //if (c >= '0' && c <= '9' ||
                        //    c == '-' && i + 1 < length)
                        //{
                        //    continue;
                        //}

                        //if (throwOnError)
                        //{
                        //    throw new FormatException(string.Format("{0} at position {1} is not an expected character in {2}", c, i, expression));
                        //}

                        //result = double.NaN;
                        //return false;

                        continue;
                    }

                    if (c != '-')
                    {
                        valueStarted = true;
                    }

                    if (i + 1 < length) // not end of string
                    {
                        // continue reading value
                        continue;
                    }
                }
                else if (stack.Count == 0 && !valueStarted && c != '(')
                {
                    if (throwOnError)
                    {
                        throw new FormatException(string.Format("{0} at position {1} is not an expected character in {2}", c, i, expression));
                    }

                    result = double.NaN;
                    return false;
                }

                // value complete
                if (i + 1 == length)
                {
                    double value;

                    if (valueStarted)
                    {
                        var token =
                            c == ')' && parentheses == 1
                                ? expression.Substring(start, i - start)
                                : expression.Substring(start);

                        if (!double.TryParse(token, out value))
                        {
                            if (throwOnError)
                            {
                                // need to retry parsing to throw with meaningful syntax error
                                double.Parse(token);
                            }

                            result = double.NaN;
                            return false;
                        }
                    }
                    else
                    {
                        if (stack.Count == 0)
                        {
                            if (throwOnError)
                            {
                                // need to retry parsing to throw with meaningful syntax error
                                throw new FormatException(string.Format("{0} at position {1} is not an expected character in {2}", c, i, expression));
                            }

                            result = double.NaN;
                            return false;
                        }

                        value = 0;
                    }

                    while (stack.Count > 0)
                    {
                        value = stack.Pop().GetResult(value);
                    }

                    if (parentheses != 0 && !(c == ')' && parentheses == 1))
                    {
                        if (throwOnError)
                        {
                            // need to retry parsing to throw with meaningful syntax error
                            throw new FormatException(string.Format("Expression missing a closing parenthesis character - {0} ", expression));
                        }

                        result = double.NaN;
                        return false;
                    }

                    result = value;
                    return true;
                }
                else
                {
                    double value;

                    if (valueStarted)
                    {
                        if (!double.TryParse(expression.Substring(start, i - start), out value))
                        {
                            if (throwOnError)
                            {
                                // need to retry parsing to throw with meaningful syntax error
                                double.Parse(expression.Substring(start, i - start));
                            }

                            result = double.NaN;
                            return false;
                        }
                    }
                    else
                    {
                        value = 0;
                    }

                    start = i + 1;
                    Func<double, double, double> func = null;

                    switch (c)
                    {
                        case '+':
                            func = AddFunc;
                            break;
                        case '-':
                            func = SubtractFunc;
                            break;
                        case '*':
                            func = MultiplyFunc;
                            break;
                        case '/':
                            func = DivideFunc;
                            break;
                        case '%':
                            func = ModuloFunc;
                            break;
                        case '^':
                            func = PowerFunc;
                            break;
                        case '(':
                            if (stack.Count > 0 && stack.Peek().Function == NumberFunc ||
                                valueStarted)
                            {
                                if (throwOnError)
                                {
                                    throw new FormatException(string.Format("{0} at position {1} is not an expected character in {2}", c, i, expression));
                                }

                                result = double.NaN;
                                return false;
                            }

                            parentheses++;
                            break;
                        case ')':
                            while (
                                stack.Count > 0 &&
                                stack.Peek().Parentheses == parentheses)
                            {
                                value = stack.Pop().GetResult(value);
                            }

                            parentheses--;

                            if (parentheses < 0)
                            {
                                if (throwOnError)
                                {
                                    throw new FormatException(string.Format("Closing unopened parenthesis at position {0} in {1}.", i, expression));
                                }

                                result = double.NaN;
                                return false;
                            }

                            if (i + 1 == length)
                            {
                                result = value;
                                return true;
                            }

                            stack.Push(new Operation(value, NumberFunc, parentheses));
                            break;
                        default:
                            if (throwOnError)
                            {
                                throw new FormatException(string.Format("{0} at position {1} is not an expected character in {2}", c, i, expression));
                            }

                            result = double.NaN;
                            return false;
                    }

                    if (func != null)
                    {
                        if (i == length - 1 ||
                            expression[i + 1] == '+' ||
                            expression[i + 1] == '-' ||
                            expression[i + 1] == '*' ||
                            expression[i + 1] == '/' ||
                            expression[i + 1] == '%' ||
                            expression[i + 1] == '^' ||
                            !valueStarted && stack.Count == 0)
                        {
                            if (throwOnError)
                            {
                                throw new FormatException(string.Format("{0} at position {1} is not an expected character in {2}", expression[i + 1], i + 1, expression));
                            }

                            result = double.NaN;
                            return false;
                        }

                        while (
                            stack.Count > 0 &&
                            stack.Peek().Parentheses == parentheses &&
                            stack.Peek().Function.IsLeftFirst(func))
                        {
                            value = stack.Pop().GetResult(value);
                        }

                        stack.Push(new Operation(value, func, parentheses));
                    }

                    valueStarted = false;
                }
            }

            if (throwOnError)
            {
                throw new InvalidOperationException();
            }

            result = double.NaN;
            return false;
        }
    }

    //public sealed class NumericUpDownAccelerationCollection : List<NumericUpDownAcceleration>
    //{
    //    //TODO: Consider adding this
    //}

    //public sealed class NumericUpDownAcceleration : DependencyObject
    //{
    //    #region Seconds
    //    /// <summary>
    //    /// Seconds Dependency Property
    //    /// </summary>
    //    public static readonly DependencyProperty SecondsProperty =
    //        DependencyProperty.Register(
    //            "Seconds",
    //            typeof(int),
    //            typeof(NumericUpDownAcceleration),
    //            new PropertyMetadata(0));

    //    /// <summary>
    //    /// Gets or sets the Seconds property. This dependency property 
    //    /// indicates the number of seconds before the acceleration takes place.
    //    /// </summary>
    //    public int Seconds
    //    {
    //        get { return (int)GetValue(SecondsProperty); }
    //        set { SetValue(SecondsProperty, value); }
    //    }
    //    #endregion

    //    #region Increment
    //    /// <summary>
    //    /// Increment Dependency Property
    //    /// </summary>
    //    public static readonly DependencyProperty IncrementProperty =
    //        DependencyProperty.Register(
    //            "Increment",
    //            typeof(double),
    //            typeof(NumericUpDownAcceleration),
    //            new PropertyMetadata(0));

    //    /// <summary>
    //    /// Gets or sets the Increment property. This dependency property 
    //    /// indicates the increment that takes place every button click
    //    /// after the button has been pressed for the duration specified
    //    /// by the Seconds property.
    //    /// </summary>
    //    public double Increment
    //    {
    //        get { return (double)GetValue(IncrementProperty); }
    //        set { SetValue(IncrementProperty, value); }
    //    }
    //    #endregion
    //}
}