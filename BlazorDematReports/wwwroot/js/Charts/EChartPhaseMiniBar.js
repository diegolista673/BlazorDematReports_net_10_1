function CreateEChartPhaseMiniBar(elementId, jsonData) {
    var element = document.getElementById(elementId);
    if (!element) { console.warn('Element with id ' + elementId + ' not found'); return; }
    var existingChart = echarts.getInstanceByDom(element);
    if (existingChart) existingChart.dispose();
    var myChart = echarts.init(element, null, { renderer: 'svg' });
    var resultData = JSON.parse(jsonData || '[]');

    // Prepara etichette e serie (Documenti, Fogli, Pagine)
    var labels = []; var documentsData = []; var sheetsData = []; var pagesData = [];
    for (var i = 0; i < resultData.length; i++) {
        var item = resultData[i];
        var monthNum = parseInt(item.Mese) || i + 1;
        var meseNome = new Date(2024, (monthNum > 0 ? monthNum - 1 : 0), 1).toLocaleString('it-IT', { month: 'short' });
        labels.push(meseNome);
        documentsData.push(parseInt(item.Documenti) || 0);
        sheetsData.push(parseInt(item.Fogli) || 0);
        pagesData.push(parseInt(item.Pagine) || 0);
    }

    var option = {
        animation: false,
        grid: { left: 2, top: 2, right: 2, bottom: 2, containLabel: false },
        tooltip: {
            trigger: 'axis', axisPointer: { type: 'shadow' },
            formatter: function (params) {
                var res = params[0].name + '<br/>';
                params.forEach(function (p) { res += p.marker + ' ' + p.seriesName + ': ' + p.value.toLocaleString() + '<br/>'; });
                return res;
            }
        },
        legend: { show: false },
        xAxis: { type: 'category', data: labels, show: false },
        yAxis: { type: 'value', show: false },
        series: [
            { name: 'Pag', type: 'bar', stack: 'total', data: pagesData, barMaxWidth: 12, itemStyle: { color: '#0d47a1' } },
            { name: 'Fogli', type: 'bar', stack: 'total', data: sheetsData, barMaxWidth: 12, itemStyle: { color: '#1976d2' } },
            { name: 'Doc', type: 'bar', stack: 'total', data: documentsData, barMaxWidth: 12, itemStyle: { color: '#90caf9' } }
        ]
    };
    myChart.setOption(option);
    window.addEventListener('resize', function () { if (myChart && !myChart.isDisposed()) myChart.resize(); });
}

// Batch rendering: expects JSON array of { elementId, data: [{Mese, Documenti, Fogli, Pagine}] }
function CreateEChartPhaseMiniBarBatch(jsonBatch) {
    if (!jsonBatch) return; var batch = JSON.parse(jsonBatch); if (!Array.isArray(batch)) return;
    for (var i = 0; i < batch.length; i++) {
        var item = batch[i]; var elementId = item.elementId; var data = item.data || [];
        var element = document.getElementById(elementId); if (!element) { console.warn('Element ' + elementId + ' not found'); continue; }
        var existingChart = echarts.getInstanceByDom(element); if (existingChart) existingChart.dispose();
        var myChart = echarts.init(element, null, { renderer: 'svg' });
        var labels = []; var documentsData = []; var sheetsData = []; var pagesData = [];
        for (var j = 0; j < data.length; j++) {
            var row = data[j]; var monthNum = parseInt(row.Mese) || (j + 1);
            var meseNome = new Date(2024, (monthNum > 0 ? monthNum - 1 : 0), 1).toLocaleString('it-IT', { month: 'short' });
            labels.push(meseNome);
            documentsData.push(parseInt(row.Documenti) || 0);
            sheetsData.push(parseInt(row.Fogli) || 0);
            pagesData.push(parseInt(row.Pagine) || 0);
        }
        var option = {
            animation: false,
            grid: { left: 2, top: 2, right: 2, bottom: 2, containLabel: false },
            tooltip: {
                trigger: 'axis', axisPointer: { type: 'shadow' },
                formatter: function (params) {
                    var res = params[0].name + '<br/>';
                    params.forEach(function (p) { res += p.marker + ' ' + p.seriesName + ': ' + p.value.toLocaleString() + '<br/>'; });
                    return res;
                }
            },
            legend: { show: false },
            xAxis: { type: 'category', data: labels, show: false },
            yAxis: { type: 'value', show: false },
            series: [
                { name: 'Pag', type: 'bar', stack: 'total', data: pagesData, barMaxWidth: 12, itemStyle: { color: '#0d47a1' } },
                { name: 'Fogli', type: 'bar', stack: 'total', data: sheetsData, barMaxWidth: 12, itemStyle: { color: '#1976d2' } },
                { name: 'Doc', type: 'bar', stack: 'total', data: documentsData, barMaxWidth: 12, itemStyle: { color: '#90caf9' } }
            ]
        };
        myChart.setOption(option);
    }
    window.addEventListener('resize', function () {
        for (var i = 0; i < batch.length; i++) {
            var el = document.getElementById(batch[i].elementId); if (!el) continue;
            var chart = echarts.getInstanceByDom(el); if (chart && !chart.isDisposed()) chart.resize();
        }
    });
}
