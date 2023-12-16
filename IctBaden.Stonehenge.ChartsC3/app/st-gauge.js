

updated: function () {

    if(typeof(this.gauge) == "undefined") this.gauge = c3.generate({
        bindto: this.$el,
        data: {
            columns: [
                [this.$props.gaugedata.Label, this.$props.gaugedata.Value]
            ],
            type: 'gauge',
            onclick: function(d, i) { console.log("onclick", d, i); },
            onmouseover: function (d, i) { console.log("onmouseover", d, i); },
            onmouseout: function (d, i) { console.log("onmouseout", d, i); }
        },
        gauge: {
            label: {
                format: function (value, ratio) {
                    return value;
                },
                show: this.$props.gaugedata.MinMaxLabels // to turn off the min/max labels.
            },
            min: this.$props.gaugedata.Min, // 0 is default, //can handle negative min e.g. vacuum / voltage / current flow / rate of change
            max: this.$props.gaugedata.Max, // 100 is default
            units: this.$props.gaugedata.Units,
            width: this.$props.gaugedata.Thickness // for adjusting arc thickness
        },
        //chartColor: '#0000FF',
        color: {
            pattern: [...this.$props.gaugedata.ColorPatterns], // the color levels for the percentage values.
            threshold: { values: [...this.$props.gaugedata.ColorThresholds] }
        },
        size: {
            height: '100%'
        }
    });

    this.gauge.load({
        columns: [[this.$props.gaugedata.Label, this.$props.gaugedata.Value]]
    });

}