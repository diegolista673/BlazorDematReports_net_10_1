function CreateEChartPhaseLast5Years(elementId, jsonData) {
    // Controlla se l'elemento esiste
    var element = document.getElementById(elementId);
    if (!element) {
        console.warn('Element with id ' + elementId + ' not found');
        return;
    }

    // Dispose del grafico esistente se presente
    var existingChart = echarts.getInstanceByDom(element);
    if (existingChart) {
        existingChart.dispose();
    }

    // Inizializza il grafico
    var myChart = echarts.init(element, null, { renderer: 'svg' });

    // Parse dei dati JSON
    var resultData = JSON.parse(jsonData);

    var years = [];
    var documentsData = [];
    var sheetsData = [];
    var pagesData = [];

    // Raggruppa i dati per anno
    var dataByYear = {};
    for (var i = 0; i < resultData.length; i++) {
        var item = resultData[i];
        var year = item["Anno"];
        
        if (!dataByYear[year]) {
            dataByYear[year] = {
                documenti: 0,
                fogli: 0,
                pagine: 0
            };
        }
        
        dataByYear[year].documenti += parseInt(item["Documenti"]) || 0;
        dataByYear[year].fogli += parseInt(item["Fogli"]) || 0;
        dataByYear[year].pagine += parseInt(item["Pagine"]) || 0;
    }

    // Ordina gli anni e prendi gli ultimi 5
    var sortedYears = Object.keys(dataByYear).sort().slice(-5);
    
    for (var j = 0; j < sortedYears.length; j++) {
        var year = sortedYears[j];
        years.push(year);
        documentsData.push(dataByYear[year].documenti);
        sheetsData.push(dataByYear[year].fogli);
        pagesData.push(dataByYear[year].pagine);
    }

    // Configurazione del grafico
    var option = {
        animation: false,
        grid: {
            left: 30,
            top: 10,
            right: 10,
            bottom: 20,
            containLabel: false
        },
        tooltip: {
            trigger: 'axis',
            axisPointer: {
                type: 'shadow'
            },
            formatter: function(params) {
                var result = params[0].name + '<br/>';
                params.forEach(function(item) {
                    result += item.marker + ' ' + item.seriesName + ': ' + 
                             item.value.toLocaleString() + '<br/>';
                });
                return result;
            }
        },
        legend: {
            show: false
        },
        xAxis: {
            type: 'category',
            data: years,
            axisLabel: {
                fontSize: 10,
                rotate: 0
            },
            axisLine: {
                show: true,
                lineStyle: {
                    color: '#ccc'
                }
            }
        },
        yAxis: {
            type: 'value',
            show: false,
            axisLabel: {
                fontSize: 10,
                formatter: function (value) {
                    if (value >= 1000000) {
                        return (value / 1000000).toFixed(1) + 'M';
                    } else if (value >= 1000) {
                        return (value / 1000).toFixed(0) + 'K';
                    } else {
                        return value;
                    }
                }
            }
        },
        series: [
            {
                name: 'Pag',
                type: 'bar',
                stack: 'total',
                data: pagesData,
                //itemStyle: {
                //    color: '#594ae2'  // Primary color
                //},
                barMaxWidth: 25,
                itemStyle: { color: '#5e60e6' }
            },
            {
                name: 'Fogli',
                type: 'bar',
                stack: 'total',
                data: sheetsData,
                //itemStyle: {
                //    color: '#ff6090'  // Secondary color
                //},
                barMaxWidth: 25,
                itemStyle: { color: '#e5e833' }
            },
            {
                name: 'Doc',
                type: 'bar',
                stack: 'total',
                data: documentsData,
                //itemStyle: {
                //    color: '#00c853'  // Success color
                //},
                barMaxWidth: 25,
                itemStyle: { color: '#5ee667' }
            }
        ]
    };

    // Imposta le opzioni del grafico
    myChart.setOption(option);

    // Resize handler
    window.addEventListener('resize', function () {
        if (myChart && !myChart.isDisposed()) {
            myChart.resize();
        }
    });
}

// Batch rendering per grafici ultimi 5 anni
// Expects: [{ elementId, data: [{ Anno, Documenti, Fogli, Pagine }] }]
function CreateEChartPhaseLast5YearsBatch(jsonBatch) {
    if (!jsonBatch) return;
    var batch = JSON.parse(jsonBatch);
    if (!Array.isArray(batch)) return;
    
    for (var i = 0; i < batch.length; i++) {
        var item = batch[i];
        var elementId = item.elementId;
        var resultData = item.data || [];
        
        var element = document.getElementById(elementId);
        if (!element) {
            console.warn('Element ' + elementId + ' not found');
            continue;
        }
        
        var existingChart = echarts.getInstanceByDom(element);
        if (existingChart) existingChart.dispose();
        
        var myChart = echarts.init(element, null, { renderer: 'svg' });
        
        // Aggrega dati per anno
        var dataByYear = {};
        for (var j = 0; j < resultData.length; j++) {
            var dataItem = resultData[j];
            var year = dataItem.Anno;
            if (!dataByYear[year]) {
                dataByYear[year] = { documenti: 0, fogli: 0, pagine: 0 };
            }
            dataByYear[year].documenti += parseInt(dataItem.Documenti) || 0;
            dataByYear[year].fogli += parseInt(dataItem.Fogli) || 0;
            dataByYear[year].pagine += parseInt(dataItem.Pagine) || 0;
        }
        
        // Ordina e prendi ultimi 5 anni
        var sortedYears = Object.keys(dataByYear).sort().slice(-5);
        var years = [];
        var documentsData = [];
        var sheetsData = [];
        var pagesData = [];
        
        for (var k = 0; k < sortedYears.length; k++) {
            var year = sortedYears[k];
            years.push(year);
            documentsData.push(dataByYear[year].documenti);
            sheetsData.push(dataByYear[year].fogli);
            pagesData.push(dataByYear[year].pagine);
        }
        
        var option = {
            animation: false,
            grid: { left: 30, top: 10, right: 10, bottom: 20, containLabel: false },
            tooltip: {
                trigger: 'axis',
                axisPointer: { type: 'shadow' },
                formatter: function(params) {
                    var result = params[0].name + '<br/>';
                    params.forEach(function(p) {
                        result += p.marker + ' ' + p.seriesName + ': ' + p.value.toLocaleString() + '<br/>';
                    });
                    return result;
                }
            },
            legend: { show: false },
            xAxis: {
                type: 'category',
                data: years,
                axisLabel: { fontSize: 9, rotate: 0 },
                axisLine: { show: true, lineStyle: { color: '#ccc' } }
            },
            yAxis: { type: 'value', show: false },
            series: [
                { name: 'Pag', type: 'bar', stack: 'total', data: pagesData, barMaxWidth: 20, itemStyle: { color: '#5e60e6' } },
                { name: 'Fogli', type: 'bar', stack: 'total', data: sheetsData, barMaxWidth: 20, itemStyle: { color: '#5ee667' } },
                { name: 'Doc', type: 'bar', stack: 'total', data: documentsData, barMaxWidth: 20, itemStyle: { color: '#e83333' } }
            ]
        };
        
        myChart.setOption(option);
    }
    
    // Single resize hook per tutti i grafici
    window.addEventListener('resize', function () {
        for (var i = 0; i < batch.length; i++) {
            var el = document.getElementById(batch[i].elementId);
            if (!el) continue;
            var chart = echarts.getInstanceByDom(el);
            if (chart && !chart.isDisposed()) chart.resize();
        }
    });
}
