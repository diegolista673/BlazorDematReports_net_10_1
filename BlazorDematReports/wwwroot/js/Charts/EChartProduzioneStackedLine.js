function CreateEChartStackedLine(jsonData, titleText, showLegend) {
    var element = document.getElementById('mainChartLine');
    if (!element) { console.warn('mainChartLine element missing'); return; }

    // Dispose previous chart instance if any
    var existing = echarts.getInstanceByDom(element);
    if (existing) existing.dispose();

    var data = [];
    try { data = JSON.parse(jsonData) || []; } catch(e){ console.error('Invalid JSON for stacked line', e); return; }
    if (!Array.isArray(data) || data.length === 0) { console.info('No data for stacked line chart'); return; }

    // Expect objects with Periodo, Documenti, Fogli, Ore
    var categories = []; var serieDocumenti = []; var serieFogli = []; var serieOre = [];
    for (var i=0;i<data.length;i++) {
        var item = data[i];
        categories.push(item.Periodo || '');
        serieDocumenti.push(parseInt(item.Documenti) || 0);
        serieFogli.push(parseInt(item.Fogli) || 0);
        // Ore may be double
        serieOre.push(parseFloat(item.Ore) || 0);
    }

    var myChart = echarts.init(element, null, { renderer: 'svg' });

    var option = {
        animation: false,
        grid: { left: 40, right: 20, top: 30, bottom: 30, containLabel: false },
        tooltip: {
            trigger: 'axis',
            axisPointer: { type: 'line' },
            formatter: function(params){
                var total = 0; var res = params[0].axisValue + '<br/>';
                params.forEach(function(p){
                    total += (p.value || 0);
                    res += p.marker + ' ' + p.seriesName + ': ' + (p.value || 0).toLocaleString() + '<br/>'; 
                });
                res += '<b>TOT: ' + total.toLocaleString() + '</b>';
                return res;
            }
        },
        legend: { show: !!showLegend },
        xAxis: {
            type: 'category',
            data: categories,
            boundaryGap: false,
            axisLabel: { fontSize: 10 },
            axisLine: { lineStyle: { color: '#ccc' } }
        },
        yAxis: { type: 'value', show: false },
        title: titleText ? { text: titleText, left: 'center', top: 0 } : undefined,
        series: [
            { name: 'Pagine', type: 'line', stack: 'total', smooth: false, symbol: 'circle', showSymbol: false, data: serieDocumenti, areaStyle: {}, lineStyle: { width: 1 }, color: '#5e60e6' },
            { name: 'Fogli', type: 'line', stack: 'total', smooth: false, symbol: 'circle', showSymbol: false, data: serieFogli, areaStyle: {}, lineStyle: { width: 1 }, color: '#e5e833' },
            { name: 'Ore', type: 'line', stack: 'total', smooth: false, symbol: 'circle', showSymbol: false, data: serieOre, areaStyle: {}, lineStyle: { width: 1 }, color: '#5ee667' }
        ]
    };

    myChart.setOption(option);

    function resizeHandler(){ if (myChart && !myChart.isDisposed()) myChart.resize(); }
    window.addEventListener('resize', resizeHandler);
}
