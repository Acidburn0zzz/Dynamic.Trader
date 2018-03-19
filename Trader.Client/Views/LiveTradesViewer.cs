﻿using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using DynamicData.PLinq;
using Trader.Domain.Model;
using Trader.Domain.Services;

namespace Trader.Client.Views
{
    public class LiveTradesViewer :AbstractNotifyPropertyChanged, IDisposable
    {
        private readonly IDisposable _cleanUp;
        private readonly ReadOnlyObservableCollection<TradeProxy> _data;

        public LiveTradesViewer(ITradeService tradeService,SearchHints searchHints)
        {
            SearchHints = searchHints;

            var filter = SearchHints.WhenValueChanged(t => t.SearchText)
                        .Select(BuildFilter);

            var loader = tradeService.Live.Connect()
                .Filter(filter) // apply user filter
                //if targeting dotnet 4.5 can parallelise 'cause it's quicker
                .Transform(trade => new TradeProxy(trade))
                .Sort(SortExpressionComparer<TradeProxy>.Descending(t => t.Timestamp),SortOptimisations.ComparesImmutableValuesOnly, 25)
                .ObserveOnDispatcher()
                .Bind(out _data)   // update observable collection bindings
                .DisposeMany() //since TradeProxy is disposable dispose when no longer required
                .Subscribe();

            _cleanUp = new CompositeDisposable(loader, searchHints);
        }

        private Func<Trade, bool> BuildFilter(string searchText)
        {
            if (string.IsNullOrEmpty(searchText)) return trade => true;

            return t => t.CurrencyPair.Contains(searchText, StringComparison.OrdinalIgnoreCase) 
                            || t.Customer.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        public ReadOnlyObservableCollection<TradeProxy> Data => _data;

        public SearchHints SearchHints { get; }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }
    }
}