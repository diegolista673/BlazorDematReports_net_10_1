function CreateEChartStackedLine(jsonData, titleText, shouldAdjustGrid) {

    var myChart = echarts.init(document.getElementById('mainChartLine'), null, { renderer: 'svg' });

    window.addEventListener('resize', function () {
        myChart.resize();
    });

    var resultData = JSON.parse(jsonData);
    var gridConfig;
    var legendConfig;
    
    if (shouldAdjustGrid) {
        gridConfig = [
            { bottom: '52%', top: '22%', right: '5%' }, 
            { bottom: '10%', top: '60%', right: '5%' }  
        ];

        legendConfig = [
            { top: '10%' },
        ];
    } else {
        gridConfig = [
            { bottom: '60%', top: '12%' }, 
            { bottom: '10%', top: '60%' }  
        ];

        legendConfig = [
            { top: '0%' },
        ];
    }

    // Specify the configuration items and data for the stacked bar chart
    if (resultData.length === 0) {
        option = {
            graphic: {
                type: 'image',
                left: 'center',
                top: 'middle',
                style: {
                    image: '/images/no-data-icon-10.png', // Placeholder image URL
                    width: 350,
                    height: 350,
                }
            }
        };
        myChart.setOption(option, true); // Set option for no data image
    } else {
        option = {
            legend: legendConfig,
            title: {
                show: true,
                text: titleText,
                left: 'center'
            },
            tooltip: {
                trigger: 'axis'
            },
            toolbox: {
                feature: {
                    saveAsImage: {}
                }
            },
            dataset: {
                source: resultData
            },
            xAxis: [
                { type: 'category', boundaryGap: true, gridIndex: 0 },
                { type: 'category', boundaryGap: true, gridIndex: 1 }
            ],
            yAxis: [
                {
                    gridIndex: 0,
                    scale: true,
                    axisLabel: {
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
                {
                    gridIndex: 1,
                    scale: true,
                    axisLabel: {
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
                }
            ],
            grid: gridConfig,
            series: [
                { type: 'line', seriesLayoutBy: 'column', xAxisIndex: 0, yAxisIndex: 0, symbol: 'circle' },
                { type: 'line', seriesLayoutBy: 'column', xAxisIndex: 0, yAxisIndex: 0, symbol: 'circle' },
                { type: 'line', seriesLayoutBy: 'column', xAxisIndex: 1, yAxisIndex: 1, symbol: 'circle', itemStyle: { color: '#ff0000' } }
            ]
        };
    }

    option && myChart.setOption(option);

}



