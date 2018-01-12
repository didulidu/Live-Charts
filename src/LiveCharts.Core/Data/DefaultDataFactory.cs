﻿using System.Collections.Generic;
using System.ComponentModel;
using LiveCharts.Core.Abstractions;
using LiveCharts.Core.Config;

namespace LiveCharts.Core.Data
{
    /// <summary>
    /// Defines the default chart point factory.
    /// </summary>
    /// <seealso cref="IDataFactory" />
    public class DefaultDataFactory : IDataFactory
    {
        /// <inheritdoc />
        public IEnumerable<TPoint> FetchData<TModel, TCoordinate, TViewModel, TPoint>(
            DataFactoryArgs<TModel, TCoordinate, TViewModel, TPoint> args) 
            where TPoint : Point<TModel, TCoordinate, TViewModel>, new()
            where TCoordinate : ICoordinate
        {
            var modelType = typeof(TModel);
            var mapper = args.Series.Mapper ?? LiveChartsSettings.GetMapper<TModel, TCoordinate>();
            var notifiesChange = typeof(INotifyPropertyChanged).IsAssignableFrom(modelType);
            var observable = typeof(IChartPoint<TModel, TCoordinate, TViewModel, TPoint>).IsAssignableFrom(modelType);
            var collection = args.Collection;
            var dimensions = args.Chart.GetSeriesDimensions(args.Series);

            for (var index = 0; index < collection.Count; index++)
            {
                var instance = collection[index];

                // if INPC then attach the updater...
                if (notifiesChange)
                {
                    var npc = (INotifyPropertyChanged) instance;
                    npc.PropertyChanged += args.PropertyChangedEventHandler;
                }

                TPoint chartPoint;

                if (observable)
                {
                    var iocp = (IChartPoint<TModel, TCoordinate, TViewModel, TPoint>) instance;
                    if (iocp.ChartPoint == null)
                    {
                        chartPoint = new TPoint();
                        iocp.ChartPoint = chartPoint;
                    }
                    else
                    {
                        chartPoint = iocp.ChartPoint;
                    }
                }
                else
                {
                    if (args.Series.ValueTracker.Count < index)
                    {
                        chartPoint = args.Series.ValueTracker[index];
                    }
                    else
                    {
                        chartPoint = new TPoint();
                        args.Series.ValueTracker.Add(chartPoint);
                    }
                }

                if (chartPoint.Coordinate.CompareDimensions(dimensions, SeriesSkipCriteria.None)) continue;

                // feed our chart points ...
                chartPoint.Model = instance;
                chartPoint.Key = index;
                chartPoint.Series = args.Series;
                chartPoint.Chart = args.Chart;
                chartPoint.Coordinate = mapper.Predicate(instance, index);

                // evaluate model defined events
                mapper.EvaluateModelDependentActions(instance, chartPoint.View, chartPoint);

                // register our chart point at the resource collector
                args.Chart.RegisterResource(chartPoint);

                yield return chartPoint;
            }
        }
    }
}
