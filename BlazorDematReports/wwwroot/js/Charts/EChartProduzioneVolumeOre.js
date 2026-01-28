function CreateEChartProduzioneOre(jsonData, startDate, endDate, volume) {

    var myChart = echarts.init(document.getElementById('mainChartOre'), null, { renderer: 'svg' });

    window.addEventListener('resize', function () {
        myChart.resize();
    });

    var resultData = JSON.parse(jsonData);

    var dataSet = [];
    for (var i = 0; i < resultData.length; i++) {
        var item = resultData[i];
        dataSet.push({
            name: item['ProceduraCliente'],
            value: item['TempoLavOreCent']
        });
    }



    var option = {
        title: {
            text: "\n" + 'Periodo dal ' + startDate + ' al ' + endDate + "\n" + "\n" + 'Totale Ore : ' + volume,
            left: 'center'
        },
        tooltip: {
            trigger: 'item'
        },
        toolbox: {
            show: true,
            feature: {
                saveAsImage: {
                    title: 'save as image',
                    name: 'OreProduzione'
                }
            }
        },
        series: [
            {
                label: {
                    alignTo: 'edge',
                    formatter: '{b}\n {d}%',
                    minMargin: 5,
                    edgeDistance: 20,
                    lineHeight: 15
                },
                labelLine: {
                    length: 10,
                    length2: 0,
                    maxSurfaceAngle: 80
                },
                labelLayout: function (params) {
                    const isLeft = params.labelRect.x < myChart.getWidth() / 2;
                    const points = params.labelLinePoints;
                    // Update the end point.
                    points[2][0] = isLeft
                        ? params.labelRect.x
                        : params.labelRect.x + params.labelRect.width;
                    return {
                        labelLinePoints: points
                    };
                },
                type: 'pie',
                radius: ['0%', '30%'],
                center: ['50%', '50%'],
                data: dataSet.sort(function (a, b) {
                    return a.value - b.value;
                }),
                emphasis: {
                    itemStyle: {
                        shadowBlur: 10,
                        shadowOffsetX: 0,
                        shadowColor: 'rgba(0, 0, 0, 0.5)'
                    }
                }
            }
        ]
    };

    option && myChart.setOption(option);



}



