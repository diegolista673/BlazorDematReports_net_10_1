function CreateEChartProduzioneAnnua(jsonData) {
    /*echartsInstance.dispose;*/
    var resultData = JSON.parse(jsonData);

    var category = [];
    var value = [];
    for (var i = 0; i < resultData.length; i++) {
        var item = resultData[i];
        category.push("\"" + item["MeseString"] + "\"");
        value.push("\"" + item["Documenti"] + "\"");
    }
    category = '[' + category + ']';
    var result_category = category.toString();

    value = '[' + value + ']';
    var result_value = value.toString();

    var myChart = echarts.init(document.getElementById('mainChart'), null, { renderer: 'svg' });
/*    var myChart = echarts.init(document.getElementById('mainChart'));*/


    window.addEventListener('resize', function () {
        myChart.resize();
    });



    // Specify the configuration items and data for the chart
    var option = {
        aria: {
            enable: true,
            decal : {
                show:true
            }
        },
        legend: {},
        tooltip: {},
        title: {
            show: true
        },
        grid: {
            left: 10,
            top: 30,
            right: 10,
            bottom: 30
        },
        toolbox: {
            show: true,
            feature: {
                saveAsImage: {
                    title: 'save as image',
                    name: 'ProduzioneAnnua'
                }
            }
        },
        xAxis: {
            data: JSON.parse(result_category)
        },
        yAxis: {
            axisLabel:{
                show:false
            }
        },
        series: [{
            
            type: 'bar',
            data: JSON.parse(result_value)
        }],

    };

    // Display the chart using the configuration items and data just specified.
    myChart.setOption(option);
}



