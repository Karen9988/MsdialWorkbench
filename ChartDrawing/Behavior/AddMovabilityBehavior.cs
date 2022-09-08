﻿using CompMs.Graphics.Core.Adorner;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Behavior
{
    public static class AddMovabilityBehavior
    {
        public static readonly DependencyProperty PositionBaseProperty =
            DependencyProperty.RegisterAttached(
                "PositionBase",
                typeof(FrameworkElement),
                typeof(AddMovabilityBehavior),
                new PropertyMetadata(
                    null,
                    OnPositionBaseChanged));
        public static FrameworkElement GetPositionBase(DependencyObject d)
            => (FrameworkElement)d.GetValue(PositionBaseProperty);
        public static void SetPositionBase(DependencyObject d, FrameworkElement value)
            => d.SetValue(PositionBaseProperty, value);

        private static readonly DependencyProperty IsMovingProperty =
            DependencyProperty.RegisterAttached("IsMoving", typeof(bool), typeof(AddMovabilityBehavior));
        private static bool GetIsMoving(DependencyObject d)
            => (bool)d.GetValue(IsMovingProperty);
        private static void SetIsMoving(DependencyObject d, bool value)
            => d.SetValue(IsMovingProperty, value);

        private static readonly DependencyProperty MoveInitialPointProperty =
            DependencyProperty.RegisterAttached("MoveInitialPoint", typeof(Point), typeof(AddMovabilityBehavior));
        private static Point GetMoveInitialPoint(DependencyObject d)
            => (Point)d.GetValue(MoveInitialPointProperty);
        private static void SetMoveInitialPoint(DependencyObject d, Point value)
            => d.SetValue(MoveInitialPointProperty, value);

        private static readonly DependencyProperty MoveInitialMatrixProperty =
            DependencyProperty.RegisterAttached("MoveInitialMatrix", typeof(Matrix), typeof(AddMovabilityBehavior));
        private static Matrix GetMoveInitialMatrix(DependencyObject d)
            => (Matrix)d.GetValue(MoveInitialMatrixProperty);
        private static void SetMoveInitialMatrix(DependencyObject d, Matrix value)
            => d.SetValue(MoveInitialMatrixProperty, value);

        private static readonly DependencyProperty DragRubberProperty =
            DependencyProperty.RegisterAttached("DragRubber", typeof(RubberAdorner), typeof(AddMovabilityBehavior));
        private static RubberAdorner GetDragRubber(DependencyObject d)
            => (RubberAdorner)d.GetValue(DragRubberProperty);
        private static void SetDragRubber(DependencyObject d, RubberAdorner value)
            => d.SetValue(DragRubberProperty, value);

        private static readonly DependencyProperty DragInitialPointProperty =
            DependencyProperty.RegisterAttached("DragInitialPoint", typeof(Point), typeof(AddMovabilityBehavior));
        private static Point GetDragInitialPoint(DependencyObject d)
            => (Point)d.GetValue(DragInitialPointProperty);
        private static void SetDragInitialPoint(DependencyObject d, Point value)
            => d.SetValue(DragInitialPointProperty, value);


        private static void OnPositionBaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement fe) {
                if (e.OldValue != null) {
                    OnDetached(fe);
                }
                if (e.NewValue != null) {
                    OnAttaching(fe);
                }
            }
        }

        private static void OnDetached(FrameworkElement fe) {
            fe.RenderTransform = null;
            fe.MouseWheel -= OnMouseWheel;
            fe.MouseMove -= OnMouseMove;
            fe.MouseLeave -= OnMouseLeave;
            fe.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            fe.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            fe.MouseRightButtonUp -= OnMouseRightButtonUp;
            fe.MouseRightButtonDown -= OnMouseRightButtonDown;
        }

        private static void OnAttaching(FrameworkElement fe) {
            fe.RenderTransform = new MatrixTransform(new Matrix(1, 0, 0, 1, 0, 0));
            fe.MouseWheel += OnMouseWheel;
            fe.MouseMove += OnMouseMove;
            fe.MouseLeave += OnMouseLeave;
            fe.MouseLeftButtonUp += OnMouseLeftButtonUp;
            fe.MouseLeftButtonDown += OnMouseLeftButtonDown;
            fe.MouseRightButtonUp += OnMouseRightButtonUp;
            fe.MouseRightButtonDown += OnMouseRightButtonDown;
        }

        private static void OnMouseWheel(object sender, MouseWheelEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }

            var position = e.GetPosition(fe);
            if (e.Delta > 0) {
                //var inv = _matrix;
                //inv.Invert();
                //position = inv.Transform(position);
                var matrix = GetTransformMatrix(fe);
                matrix.ScaleAt(1/1.1, 1/1.1, position.X, position.Y);
                //_matrix = _matrix * new Matrix(.9, 0, 0, .9, position.X, position.Y);
                //var transition = (position - new Point(ActualWidth / 2, ActualHeight / 2)) * -.1;
                //_matrix.Translate(transition.X, transition.Y);
                //_matrix.Scale(.9, .9);
                //_matrix = new Matrix(_matrix.M11 * .9, 0, 0, _matrix.M22 * .9, .1 * position.X + .9 * _matrix.OffsetX, .1 * position.Y + .9 * _matrix.OffsetY);
                fe.RenderTransform = new MatrixTransform(matrix);
            }
            else if (e.Delta < 0) {
                //var inv = _matrix;
                //inv.Invert();
                //position = inv.Transform(position);
                var matrix = GetTransformMatrix(fe);
                matrix.ScaleAt(1.1d, 1.1d, position.X, position.Y);
                //_matrix = _matrix * new Matrix(1.1, 0, 0, 1.1, position.X, position.Y);
                //var transition = (position - new Point(ActualWidth / 2, ActualHeight / 2)) * .1;
                //_matrix.Translate(transition.X, transition.Y);
                //_matrix.Scale(1.1, 1.1);
                //_matrix = new Matrix(_matrix.M11 * 1.1, 0, 0, _matrix.M22 * 1.1, -.1 * position.X + 1.1 * _matrix.OffsetX, -.1 * position.Y + 1.1 * _matrix.OffsetY);
                fe.RenderTransform = new MatrixTransform(matrix);
            }
        }

        private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            if (e.ClickCount == 2) {
                fe.RenderTransform = new MatrixTransform();
            }
            var matrix = GetTransformMatrix(fe);
            SetIsMoving(fe, true);
            SetMoveInitialPoint(fe, e.GetPosition(GetPositionBase(fe)));
            SetMoveInitialMatrix(fe, matrix);
        }

        private static void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            SetIsMoving(fe, false);
            SetMoveInitialPoint(fe, default);
            SetMoveInitialMatrix(fe, default);
        }

        private static void OnMouseLeave(object sender, MouseEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            SetIsMoving(fe, false);
            SetMoveInitialPoint(fe, default);
            SetMoveInitialMatrix(fe, default);
        }

        private static void OnMouseMove(object sender, MouseEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            if (GetIsMoving(fe)) {
                var transition = e.GetPosition(GetPositionBase(fe)) - GetMoveInitialPoint(fe);
                var matrix = GetMoveInitialMatrix(fe);
                matrix.Translate(transition.X, transition.Y);
                fe.RenderTransform = new MatrixTransform(matrix);
            }
            if (GetDragRubber(fe) != null) {
                var adorner = GetDragRubber(fe);
                var initial = GetDragInitialPoint(fe);
                adorner.Offset = e.GetPosition(fe) - initial;
            }
        }

        private static void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            var adorner = new RubberAdorner(fe, e.GetPosition(fe));
            adorner.Attach();
            fe.CaptureMouse();
            SetDragRubber(fe, adorner);
            SetDragInitialPoint(fe, e.GetPosition(fe));
        }

        private static void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (!(sender is FrameworkElement fe)) {
                return;
            }
            var rubber = GetDragRubber(fe);
            if (rubber != null) {
                var adorner = rubber;
                var initial = GetDragInitialPoint(fe);
                SetDragRubber(fe, null);
                fe.ReleaseMouseCapture();
                adorner.Detach();
                var transition = e.GetPosition(fe) - initial;
                var center = initial + transition / 2;
                var matrix = GetTransformMatrix(fe);
                matrix.ScaleAt(fe.ActualWidth / Math.Abs(transition.X), fe.ActualHeight / Math.Abs(transition.Y), center.X, center.Y);
                fe.RenderTransform = new MatrixTransform(matrix);
            }
        }

        private static Matrix GetTransformMatrix(FrameworkElement fe) {
            return ((MatrixTransform)fe.RenderTransform).Matrix;
        }
    }
}
