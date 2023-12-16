
mounted: function() {
    mermaid.initialize({ startOnLoad: false });
},
updated: function () {
    const ts = new Date().getTime();
    const id = 'id' + ts;
    const graphData = this.$props.graphData;
    mermaid.render(id, graphData)
        .then(({ svg, bindFunctions }) => {
        this.$el.innerHTML = svg;
    });
}
