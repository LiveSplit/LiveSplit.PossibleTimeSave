using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using LiveSplit.TimeFormatters;
using LiveSplit.UI.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiveSplit.UI.Components
{
    public class PossibleTimeSave : IComponent
    {
        protected InfoTimeComponent InternalComponent { get; set; }
        public PossibleTimeSaveSettings Settings { get; set; }
        private PossibleTimeSaveFormatter Formatter { get; set; }

        public GraphicsCache Cache { get; set; }

        public float PaddingTop { get { return InternalComponent.PaddingTop; } }
        public float PaddingLeft { get { return InternalComponent.PaddingLeft; } }
        public float PaddingBottom { get { return InternalComponent.PaddingBottom; } }
        public float PaddingRight { get { return InternalComponent.PaddingRight; } }

        public IDictionary<string, Action> ContextMenuControls
        {
            get { return null; }
        }

        public PossibleTimeSave(LiveSplitState state)
        {
            Formatter = new PossibleTimeSaveFormatter();
            InternalComponent = new InfoTimeComponent(null, null, Formatter);
            Cache = new GraphicsCache();
            Settings = new PossibleTimeSaveSettings()
            {
                CurrentState = state
            };
            state.ComparisonRenamed += state_ComparisonRenamed;
        }

        void state_ComparisonRenamed(object sender, EventArgs e)
        {
            var args = (RenameEventArgs)e;
            if (Settings.Comparison == args.OldName)
            {
                Settings.Comparison = args.NewName;
                ((LiveSplitState)sender).Layout.HasChanged = true;
            }
        }

        private void PrepareDraw(LiveSplitState state)
        {
            InternalComponent.DisplayTwoRows = Settings.Display2Rows;

            InternalComponent.NameLabel.HasShadow 
                = InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            Formatter.Accuracy = Settings.Accuracy;

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = Settings.OverrideTimeColor ? Settings.TimeColor : state.LayoutSettings.TextColor;
        }

        private void DrawBackground(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.ToArgb() != Color.Transparent.ToArgb()
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.ToArgb() != Color.Transparent.ToArgb())
            {
                var gradientBrush = new LinearGradientBrush(
                            new PointF(0, 0),
                            Settings.BackgroundGradient == GradientType.Horizontal
                            ? new PointF(width, 0)
                            : new PointF(0, height),
                            Settings.BackgroundColor,
                            Settings.BackgroundGradient == GradientType.Plain
                            ? Settings.BackgroundColor
                            : Settings.BackgroundColor2);
                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawBackground(g, state, width, VerticalHeight);
            PrepareDraw(state);
            InternalComponent.DrawVertical(g, state, width, clipRegion);
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawBackground(g, state, HorizontalWidth, height);
            PrepareDraw(state);
            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        public float VerticalHeight
        {
            get { return InternalComponent.VerticalHeight; }
        }

        public float MinimumWidth
        {
            get { return InternalComponent.MinimumWidth; }
        }

        public float HorizontalWidth
        {
            get { return InternalComponent.HorizontalWidth; }
        }

        public float MinimumHeight
        {
            get { return InternalComponent.MinimumHeight; }
        }

        public string ComponentName
        {
            get { return (Settings.TotalTimeSave ? "Total " : "") + "Possible Time Save" + (Settings.Comparison == "Current Comparison" ? "" : " (" + CompositeComparisons.GetShortComparisonName(Settings.Comparison) + ")"); }
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public void SetSettings(System.Xml.XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public System.Xml.XmlNode GetSettings(System.Xml.XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public TimeSpan? GetPossibleTimeSave(LiveSplitState state, ISegment segment, string comparison, bool live = false)
        {
            var splitIndex = state.Run.IndexOf(segment);
            var prevTime = TimeSpan.Zero;
            TimeSpan? bestSegments = state.Run[splitIndex].BestSegmentTime[state.CurrentTimingMethod];

            while (splitIndex > 0 && bestSegments != null)
            {
                var splitTime = state.Run[splitIndex - 1].Comparisons[comparison][state.CurrentTimingMethod];
                if (splitTime != null)
                {
                    prevTime = splitTime.Value;
                    break;
                }
                else
                {
                    splitIndex--;
                    bestSegments += state.Run[splitIndex].BestSegmentTime[state.CurrentTimingMethod];
                }
            }

            var time = segment.Comparisons[comparison][state.CurrentTimingMethod] - prevTime - bestSegments;

            if (live && splitIndex == state.CurrentSplitIndex)
            {
                var segmentDelta = TimeSpan.Zero - LiveSplitStateHelper.GetPreviousSegment(state, state.Run.IndexOf(segment), true, false, comparison, state.CurrentTimingMethod);
                if (segmentDelta < time)
                    time = segmentDelta;
            }

            if (time < TimeSpan.Zero)
                time = TimeSpan.Zero;

            return time;
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            var comparison = Settings.Comparison == "Current Comparison" ? state.CurrentComparison : Settings.Comparison;
            if (!state.Run.Comparisons.Contains(comparison))
                comparison = state.CurrentComparison;
            var comparisonName = CompositeComparisons.GetShortComparisonName(comparison);
            var componentName = (Settings.TotalTimeSave ? "Total " : "") + "Possible Time Save" + (Settings.Comparison == "Current Comparison" ? "" : " (" + comparisonName + ")");
            InternalComponent.LongestString = componentName;
            InternalComponent.NameLabel.Text = componentName;

            if (Settings.TotalTimeSave)
            {
                if (state.CurrentPhase == TimerPhase.Ended)
                {
                    InternalComponent.TimeValue = TimeSpan.Zero;
                }
                else                
                {
                    var totalPossibleTimeSave = state.Run
                        .Skip(state.CurrentSplitIndex)
                        .Select(x => GetPossibleTimeSave(state, x, comparison, true))
                        .Where(x => x.HasValue)
                        .Aggregate((TimeSpan?)TimeSpan.Zero, (a, b) => a + b);

                    InternalComponent.TimeValue = totalPossibleTimeSave;
                }
            }
            else
            {
                if (state.CurrentPhase == TimerPhase.Running || state.CurrentPhase == TimerPhase.Paused)
                {
                    InternalComponent.TimeValue = GetPossibleTimeSave(state, state.CurrentSplit, comparison);
                }
                else
                {
                    InternalComponent.TimeValue = null;
                }
            }

            Cache.Restart();
            Cache["NameValue"] = InternalComponent.NameLabel.Text;

            if (Cache.HasChanged)
            {
                InternalComponent.AlternateNameText.Clear();
                if (InternalComponent.InformationName.Contains("Total"))
                    InternalComponent.AlternateNameText.Add("Total Possible Time Save");
                InternalComponent.AlternateNameText.Add("Possible Time Save");
                InternalComponent.AlternateNameText.Add("Poss. Time Save");
                InternalComponent.AlternateNameText.Add("Time Save");
            }

            Cache["TimeValue"] = InternalComponent.ValueLabel.Text;

            if (invalidator != null && Cache.HasChanged)
            {
                invalidator.Invalidate(0, 0, width, height);
            }
        }

        public void Dispose()
        {
        }
    }
}
