
mounted: function() {
    if(typeof(this.$props.chartdata.Id) != "undefined") {
        this.chart = c3.generate({
            bindto: this.$el,
            id: this.$props.chartdata.Id,
            title: this.$props.chartdata.Title,
            grid: this.$props.chartdata.Grid,
            data: {
                onclick: function (d, element) {
                    //debugger;
                    let event = new CustomEvent("clickData", {
                        bubbles: true,
                        detail: d
                    });
                    element.dispatchEvent(event);
                },
                ...this.$props.chartdata.Data 
            },
            axis: this.$props.chartdata.Axis,
            point: this.$props.chartdata.Point,
            zoom: this.$props.chartdata.Zoom,
            size: {
                width: this.$el.clientWidth,
                height: this.$el.clientHeight,
            }
        });
    }
    this.chartId = this.$props.chartdata.Id;
},
updated: function () {

    if(typeof(this.chart) == "undefined" || this.chartId != this.$props.chartdata.Id) {
                
        this.chart = c3.generate({
            bindto: this.$el,
            title: this.$props.chartdata.Title,
            grid: this.$props.chartdata.Grid,
            data: {
                onclick: function (d, element) {
                    //debugger;
                    let event = new CustomEvent("clickData", {
                        bubbles: true,
                        detail: d
                    });
                    element.dispatchEvent(event);
                },
                ...this.$props.chartdata.Data 
            },
            axis: this.$props.chartdata.Axis,
            point: this.$props.chartdata.Point,
            zoom: this.$props.chartdata.Zoom,
            size: {
                width: this.$el.clientWidth,
                height: this.$el.clientHeight,
            }
        });
        this.chartId = this.$props.chartdata.Id;
    }

    this.chart.load({
        columns: this.$props.chartdata.Data.columns,
        onclick: function (d, element) { emit('clickData', d, element); }
        
    });

}
