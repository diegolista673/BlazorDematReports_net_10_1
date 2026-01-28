function CreateEChartStackedProduzioneAnniPrecedenti(jsonData, titleText) {
    /*echartsInstance.dispose;*/
    var resultData = JSON.parse(jsonData);

    var years = [];
    var documentsData = [];
    var sheetsData = [];
    var pagesData = [];

    for (var i = 0; i < resultData.length; i++) {
        var item = resultData[i];
        years.push(item["Anno"]);
        documentsData.push(item["Documenti"] ? parseInt(item["Documenti"]) : 0);
        sheetsData.push(item["Fogli"] ? parseInt(item["Fogli"]) : 0);
        pagesData.push(item["Pagine"] ? parseInt(item["Pagine"]) : 0);
    }

    var myChart = echarts.init(document.getElementById('ChartAnniPrecedenti'), null, { renderer: 'svg' });

    window.addEventListener('resize', function () {
        myChart.resize();
    });

    var option

    // Specify the configuration items and data for the stacked bar chart
    if (resultData.length === 0 ) {
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
            title: {
                show: true,
                text: titleText,
                left: 'center'
            },
            graphic: {
                type: 'image',
                left: 'center',
                top: 'middle',
                style: {
                    image: '',                   
                }
            },
            aria: {
                enable: true,
                decal: {
                    show: false
                }
            },
            tooltip: {
                trigger: 'axis',
                axisPointer: {
                    type: 'shadow'
                }
            },
            grid: {
                top: '10%',
                left: '3%',
                right: '10%',
                bottom: '3%',
                containLabel: true
            },
            xAxis: {
                type: 'value',
                name: '',
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
            //xAxis: {
            //    type: 'value',
            //    name: ''
            //},
            yAxis: {
                type: 'category',
                data: years
            },
            series: [
                {
                    name: 'Documenti',
                    type: 'bar',
                    stack: 'total', // Added stack: 'total'
                    emphasis: {
                        focus: 'series'
                    },
                    label: {
                        show: false
                    },
                    data: documentsData
                },
                {
                    name: 'Fogli',
                    type: 'bar',
                    stack: 'total', // Added stack: 'total'
                    emphasis: {
                        focus: 'series'
                    },
                    label: {
                        show: false
                    },
                    data: sheetsData
                },
                {
                    name: 'Pagine',
                    type: 'bar',
                    stack: 'total', // Added stack: 'total'
                    emphasis: {
                        focus: 'series'
                    },
                    label: {
                        show: false
                    },
                    data: pagesData
                }
            ]
        };
        myChart.setOption(option, true); // Set option for chart with data.
    }
}




