﻿//*********************************************************//
//    Copyright (c) Microsoft. All rights reserved.
//    
//    Apache 2.0 License
//    
//    You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//    
//    Unless required by applicable law or agreed to in writing, software 
//    distributed under the License is distributed on an "AS IS" BASIS, 
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or 
//    implied. See the License for the specific language governing 
//    permissions and limitations under the License.
//
//*********************************************************//

using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

#if NTVS_FEATURE_INTERACTIVEWINDOW
namespace Microsoft.NodejsTools.Repl {
#else
namespace Microsoft.VisualStudio.Repl {
#endif
    internal class ResizingAdorner : Adorner {
        private readonly VisualCollection _visualChildren;
        private readonly Thumb _bottomRight;

        public ResizingAdorner(UIElement adornedElement)
            : base(adornedElement) {
            _visualChildren = new VisualCollection(this);
            _bottomRight = BuildAdornerCorner(Cursors.SizeNWSE, HandleBottomRight);
        }

        private Thumb BuildAdornerCorner(Cursor cursor, DragDeltaEventHandler dragHandler) {
            var thumb = new Thumb();
            // TODO: this thumb should be styled to look like a dotted triangle, 
            // similar to the one you can see on the bottom right corner of 
            // Internet Explorer window
            thumb.Cursor = cursor;
            thumb.Height = thumb.Width = 10;
            thumb.Opacity = 0.40;
            thumb.Background = new SolidColorBrush(Colors.MediumBlue);
            thumb.DragDelta += dragHandler;
            thumb.DragStarted += (s, e) => {
                var handler = ResizeStarted;
                if (handler != null) {
                    handler(this, e);
                }
            };
            thumb.DragCompleted += (s, e) => {
                var handler = ResizeCompleted;
                if (handler != null) {
                    handler(this, e);
                }
            };
            _visualChildren.Add(thumb);
            return thumb;
        }

        private void HandleBottomRight(object sender, DragDeltaEventArgs eventArgs) {
            var thumb = sender as Thumb;
            var element = AdornedElement as FrameworkElement;
            if (element == null || thumb == null) {
                return;
            }

            element.MaxWidth = Math.Max(element.MaxWidth + eventArgs.HorizontalChange, thumb.DesiredSize.Width);
            element.MaxHeight = Math.Max(element.MaxHeight + eventArgs.VerticalChange, thumb.DesiredSize.Height);
            var size = new Size(element.MaxWidth, element.MaxHeight);
            AdornedElement.Measure(size);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            var desiredWidth = AdornedElement.DesiredSize.Width;
            var desiredHeight = AdornedElement.DesiredSize.Height;
            var adornerWidth = DesiredSize.Width;
            var adornerHeight = DesiredSize.Height;

            _bottomRight.Arrange(new Rect((desiredWidth - adornerWidth) / 2,
                (desiredHeight - adornerHeight) / 2, adornerWidth, adornerHeight));

            return finalSize;
        }

        protected override int VisualChildrenCount {
            get { return _visualChildren.Count; }
        }

        protected override Visual GetVisualChild(int index) {
            return _visualChildren[index];
        }

        public event RoutedEventHandler ResizeStarted;
        public event RoutedEventHandler ResizeCompleted;
    }
}
