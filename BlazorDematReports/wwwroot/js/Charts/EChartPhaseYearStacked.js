function CreateEChartPhaseYearStacked(elementId, jsonData) {
    var element = document.getElementById(elementId);
    if (!element) {
        console.warn('Element with id ' + elementId + ' not found');
        return;
    }

    var existingChart = echarts.getInstanceByDom(element);
    if (existingChart) {
        existingChart.dispose();
    }

    var data = [];
    try {
        data = JSON.parse(jsonData) || [];
    } catch (e) {
        console.error('Invalid JSON data for annual stacked chart', e);
        return;
    }

    if (!Array.isArray(data) || data.length === 0) {
        console.info('No data for annual stacked chart');
        return;
    }

    data.sort(function(a,b){
        return (parseInt(a.Mese) || 0) - (parseInt(b.Mese) || 0);
    });

    var months = [], docs = [], sheets = [], pages = [];
    for (var i=0;i<data.length;i++) {
        var item = data[i];
        months.push(item.MeseString || '');
        docs.push(parseInt(item.Documenti) || 0);
        sheets.push(parseInt(item.Fogli) || 0);
        pages.push(parseInt(item.Pagine) || 0);
    }

    var myChart = echarts.init(element, null, { renderer: 'svg' });

    function computeBarWidth() {
        var w = element.clientWidth || 240;
        var perCat = w / (months.length || 1);
        return Math.max(8, Math.min( perCat * 0.6, 26));
    }

    function buildOption() {
        var barW = computeBarWidth();
        return {
            grid: { left: 30, right: 10, top: 10, bottom: 24, containLabel: false },
            tooltip: {
                trigger: 'axis',
                axisPointer: { type: 'shadow' },
                formatter: function (params) {
                    var res = params[0].name + '<br/>';
                    params.forEach(function(p){
                        res += p.marker + ' ' + p.seriesName + ': ' + (p.value || 0).toLocaleString() + '<br/>';
                    });
                    var total = 0;
                    params.forEach(function(p){ total += (p.value || 0); });
                    res += '<b>TOT: ' + total.toLocaleString() + '</b>';
                    return res;
                }
            },
            legend: { show: false },
            xAxis: {
                type: 'category',
                data: months,
                axisLabel: { fontSize: 9 },
                axisLine: { lineStyle: { color: '#ccc' } },
                axisTick: { show: false }
            },
            yAxis: { type: 'value', show: false },
            series: [
                { name: 'Documenti', type: 'bar', stack: 'total', data: docs, barMaxWidth: barW },
                { name: 'Fogli', type: 'bar', stack: 'total', data: sheets, barMaxWidth: barW },
                { name: 'Pagine', type: 'bar', stack: 'total', data: pages, barMaxWidth: barW }
            ]
        };
    }

    myChart.setOption(buildOption());

    function resizeHandler() {
        if (myChart && !myChart.isDisposed()) {
            myChart.resize();
            myChart.setOption(buildOption(), false, true);
        }
    }

    window.addEventListener('resize', resizeHandler);

    if (window.ResizeObserver) {
        var ro = new ResizeObserver(function(){ resizeHandler(); });
        ro.observe(element);
    }
}
