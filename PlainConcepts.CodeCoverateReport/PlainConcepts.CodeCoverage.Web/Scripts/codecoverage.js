var username = '';
var password = '';

$(function () {
    $("#dataCoverageForm").formwizard({
        formPluginEnabled: true,
        focusFirstInput: true,
        disableUIStyles: true,
        remoteAjax: {
            "selectServer": { // add a remote ajax call when moving next from the second step
                url: "/Home/SelectCollection",
                dataType: 'json',
                success: function (data) {
                    if (data.Status == true) {

                        var select = $("<select id=\"projectUri\" name=\"projectUri\" />");

                        for (var i = 0; i < data.Projects.length; i++) {
                            $("<option />", { value: data.Projects[0].Name, text: data.Projects[0].Name }).appendTo(select);
                        }

                        $("#selectProjectLocation").html(select);
                        var url = $("#collectionUrl").val();
                        $("#collectionUrlProject").val(url);
                        localStorage.setItem('tfsUrl', url);
                        
                        activeNextStep();
                        $('#back').css('visibility', 'visible');
                        return true; //return true to make the wizard move to the next step    
                    } else {
                        $("#modalDialog").show();
                        return false; //return true to make the wizard move to the next step
                    }
                }
            },
            "selectProject": { // add a remote ajax call when moving next from the second step
                url: "/Home/SelectProject",
                dataType: 'json',
                beforeSubmit: function (data) { $("#data").html("data sent to the server: " + $.param(data)) },
                success: function (data) {
                    if (data.Status == true) {
                        var select = $("<select id=\"buildName\" name=\"buildName\" />");

                        for (var i = 0; i < data.Builds.length; i++) {
                            $("<option />", { value: data.Builds[i].BuildUri, text: data.Builds[i].Name }).appendTo(select);
                        }

                        $("#selectBuildLocation").html(select);
                        $("#collectionUrlBuild").val($("#collectionUrl").val());
                        $("#projectName").val($("#projectUri").val());
                        activeNextStep();
                        return true; //return true to make the wizard move to the next step    
                    } else {
                        return false; //return true to make the wizard move to the next step
                    }
                }
            }
        },
        formOptions: {
            success: function (data) {
                $("#codeCoverageData").hide();
                $("#codeCoverageChart").show();
                showChart('codeCoverageChart', data.Modules, data.BuildName, data.TotalCoverage);
            },
            dataType: 'json',
            resetForm: false
        }
    });
});

$(document).ready(function () {
    var url = localStorage.getItem('tfsUrl');
    if (url) {
        var control = $('#collectionUrl');
        control.val(url);
        control.select();
    }

    $("#dialog_ok").click(function () {
        $("#userNameCollection").val($("#username").val());
        $("#passwordCollection").val($("#password").val());

        $("#userNameProject").val($("#username").val());
        $("#passwordProject").val($("#password").val());

        $("#userNameBuild").val($("#username").val());
        $("#passwordBuild").val($("#password").val());
        $("#modalDialog").hide();
    });

    $("#dialog_cancel").click(function () {
        $("#modalDialog").hide();
    });

    $("#batch_ok").click(function () {
        var params = $("#parameters").val();
        $.post('/Home/BatchCoverage', { "parameters": params }).done(function (data) {
            $("#parametersContainer").hide();
            $("#codeCoverageChart").show();

            for (var indexData in data) {
                var divName = 'codeCoverageChart_' + indexData;

                var divChart = $("<div class='batchChart' id='" + divName + "' >");

                $("#codeCoverageCharts").append(divChart);
                showChart(divName, data[indexData].Modules, data[indexData].BuildName, data[indexData].TotalCoverage);
            }
        });
    });

    $('#back').click(function () {
        var activeItems = $('.step-container.active');
        activeItems.prev().addClass('active').hide().fadeIn();
        activeItems.removeClass('active');
    });
});

function showChart(divName, data, buildName, totalCoverage) {
    var series = [];
    var labels = [];

    for (var dll in data) {
        labels.push(data[dll].Key);
        var points = [];
        for (var buildIndex in data[dll].Value.Builds) {
            var build = data[dll].Value.Builds[buildIndex];
            points.push([build.Name, build.Coverage]);
        }
        series.push(points);
    }

    var plot3 = $.jqplot(divName, series,
        {
            title: buildName + ' has a coverage of ' + totalCoverage + '%',
            // Set default options on all series, turn on smoothing.
            seriesDefaults: {
                rendererOptions: {
                    smooth: true
                }
            },
            legend: {
                renderer: $.jqplot.EnhancedLegendRenderer,
                show: true,
                showLabels: true,
                location: 'e',     // compass direction, nw, n, ne, e, se, s, sw, w.
                showSwatch: true,
                placement: "outside",
                labels: labels,
                marginLeft: "30px",
                rendererOptions: { "showMarkerStyle": true, "showLineStyle": true },
                shrinkGrid: true
            },
            axes: {
                xaxis: {
                    renderer: $.jqplot.CategoryAxisRenderer,
                    label: 'build number',
                    labelRenderer: $.jqplot.CanvasAxisLabelRenderer,
                    tickRenderer: $.jqplot.CanvasAxisTickRenderer,
                    tickOptions: {
                        angle: -30,
                        fontFamily: 'Courier New',
                        fontSize: '9pt'
                    }
                }
            }
        }
    );
}

function activeNextStep() {
    var activeItems = $('.step-container.active');
    activeItems.next().addClass('active');
    activeItems.removeClass('active');
}