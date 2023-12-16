
mounted: function() {
    if(typeof(this.$props.piedata.Id) != "undefined") {
        this.pie = c3.generate({
            bindto: this.$el,
            id: this.$props.piedata.Id,
            data: this.$props.piedata.Data
        });
    }
    this.pieId = this.$props.piedata.Id;
},
updated: function () {

    if(typeof(this.pie) == "undefined" || this.pieId != this.$props.piedata.Id) {
                
        this.pie = c3.generate({
            bindto: this.$el,
            size: {
                height: this.$el.offsetHeight,
                width: this.$el.offsetWidth
            },
            data: this.$props.piedata.Data,
        });
        this.pieId = this.$props.piedata.Id;
    }

    this.pie.load({
        columns: this.$props.piedata.Data.columns
    });

}
