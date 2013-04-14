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
                        $("#collectionUrlProject").val($("#collectionUrl").val());
                        return true; //return true to make the wizard move to the next step    
                    } else {
                        alert("success!");
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
                        return true; //return true to make the wizard move to the next step    
                    } else {
                        alert("success!");
                        return false; //return true to make the wizard move to the next step
                    }
                }
            }
        },
        formOptions: {
            success: function (data) {
                $("#codeCoverageData").fadeTo(500, 0, function () {
                    showChart(data);
                });
            },
            dataType: 'json',
            resetForm: false
        }
    }
    );
});

function showChart(data) {
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

    var plot3 = $.jqplot('codeCoverageChart', series,
        {
            title: 'Code coverage',
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
                    label: 'Build number',
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