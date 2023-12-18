stonehengeViewModelName = function component() {


    let vm = {

        StonehengeCancelVmRequests: function () {
            stonehengeCancelRequests();
            this.model.StonehengePollEventsActive = null;
        },

        StonehengeSetViewModelData: function (vmData) {
            for (let propertyName in vmData) {
                if (propertyName === "StonehengeNavigate") {
                    let target = vmData[propertyName];
                    if (target.startsWith('http')) {
                        window.location.href = target;
                    } else if (target.startsWith('#')) {
                        try {
                            document.getElementById(target.substring(1))
                                .scrollIntoView({block: 'end', behaviour: 'smooth'});
                        } catch (error) {
                            // ignore
                            if (console && console.log) {
                                console.log("stonehengeViewModelName error: " + error);
                            }
                        }
                    } else {
                        app.$router.push(target);
                    }
                } else if (propertyName === "StonehengeEval") {
                    try {
                        let script = vmData[propertyName];
                        eval(script);
                    } catch (error) {
                        // ignore
                        if (console && console.log) {
                            console.log("script: " + script);
                            console.log("stonehengeViewModelName error: " + error);
                        }
                    }
                } else {
                    //debugger;
                    this.model[propertyName] = vmData[propertyName];
                }
            }
            if (app.stonehengeViewModelName.model.StonehengeInitialLoading) {
                if (typeof (stonehengeViewModelName_InitialLoaded) == 'function') {
                    try {
                        stonehengeViewModelName_InitialLoaded(app.stonehengeViewModelName.model);
                    } catch (e) {
                    }
                }
            } else {
                if (typeof (stonehengeViewModelName_DataLoaded) == 'function') {
                    try {
                        stonehengeViewModelName_DataLoaded(app.stonehengeViewModelName.model);
                    } catch (e) {
                    }
                }
            }
        },

        StonehengeSleep: function (milliseconds) {
            return new Promise(resolve => setTimeout(resolve, milliseconds));
        },

        StonehengePost: async function (urlWithParams) {
            this.StonehengeCancelVmRequests();

            let w = 3;
            while (this.StonehengePostActive && w > 0) {
                await this.StonehengeSleep(100);
            }

            let props = ['propNames'];
            let formData = {};
            props.forEach(function (prop) {
                formData[prop] = app.stonehengeViewModelName.model[prop];
            });
            this.StonehengePostActive = true;
            Vue.http.post(urlWithParams, JSON.stringify(formData),
                {
                    before(request) {
                        request.headers.set('X-Stonehenge-Id', app.stonehengeViewModelName.model.StonehengeSession);
                        //app.activeRequests.add(request);
                    }
                })
                .then(response => {
                    let data = JSON.parse(response.bodyText);
                    this.StonehengeInitialLoading = false;
                    this.StonehengeIsLoading = false;
                    if (this.StonehengePostActive) {
                        this.StonehengeSetViewModelData(data);
                        this.StonehengePostActive = false;
                    }
                    if (!this.StonehengePollEventsActive && !this.StonehengeEventSource) {
                        app.stonehengeViewModelName.RequestStonehengeEvents(true);
                    }
                })
                .catch(error => {
                    if (error.status >= 400) {
                        //debugger;
                        this.StonehengeIsDisconnected = true;
                        app.stonehengeReloadOnError(error);
                    }
                });
        },

        RequestStonehengeEvents: function (continuePolling) {

            if (!app.stonehengeViewModelName.model.StonehengePollEventsActive && !app.stonehengeViewModelName.model.StonehengeEventSource) {

                if (stonehengeUseServerSentEvents && !app.stonehengeViewModelName.model.StonehengeEventSource) {
                    if (stonehengeDebugBuild) console.log('stonehengeViewModelName request ServerSentEvents');
                    app.stonehengeViewModelName.model.StonehengeEventSource = new EventSource('EventSource/stonehengeViewModelName');
                    app.stonehengeViewModelName.model.StonehengeEventSource.onmessage = function (message) {
                        try {
                            let data = JSON.parse(message.data);
                            app.stonehengeViewModelName.StonehengeSetViewModelData(data);
                            if(!app.stonehengeViewModelName.model.StonehengeContinuePolling) {
                                if (stonehengeDebugBuild) console.log('stonehengeViewModelName stop ServerSentEvents');
                                app.stonehengeViewModelName.model.StonehengeEventSource.close();
                                app.stonehengeViewModelName.model.StonehengeEventSource = null;
                            }
                        } catch (e) {
                            console.log("RequestStonehengeEvents EX: " + e)
                            if (stonehengeDebugBuild) debugger;
                            app.stonehengeViewModelName.model.StonehengeEventSource = null;
                            app.stonehengeViewModelName.RequestStonehengeEvents(continuePolling);
                        }
                    }
                    app.stonehengeViewModelName.model.StonehengeEventSource.onerror = function (message) {
                        console.log("ServerSentEvents.OnError")
                        app.stonehengeViewModelName.model.StonehengeEventSource = null;
                    }
                    return;
                }
                if (app.stonehengeViewModelName.model.StonehengeEventSource) return;

                setTimeout(function () {
                    if (stonehengeDebugBuild) console.log('stonehengeViewModelName request RequestStonehengeEvents');
                    app.stonehengeViewModelName.StonehengePollEvents(continuePolling);
                }, app.stonehengeViewModelName.model.StonehengePollDelay);
            }

        },

        StonehengePollEvents: function (continuePolling) {
            if (!app.stonehengeViewModelName.model.StonehengeActive
                || app.stonehengeViewModelName.model.StonehengePostActive) return;
            if (app.stonehengeViewModelName.model.StonehengePollEventsActive
                || app.activeViewModelName !== 'stonehengeViewModelName') {
                //debugger;
                return;
            }
            let ts = new Date().getTime();
            Vue.http.get('Events/stonehengeViewModelName?ts=' + ts,
                {
                    before(request) {
                        app.stonehengeViewModelName.model.StonehengePollEventsActive = request;
                        request.headers.set('X-Stonehenge-Id', app.stonehengeViewModelName.model.StonehengeSession);
                        app.activeRequests.add(request);
                    }
                })
                .then(response => {
                    if (app.stonehengeViewModelName.model.StonehengePostActive) {
                        //debugger;
                        return;
                    }
                    try {
                        if (response.status >= 200 && response.status < 400) {
                            let data = JSON.parse(response.bodyText);
                            app.stonehengeViewModelName.model.StonehengePollEventsActive = null;
                            app.stonehengeViewModelName.model.StonehengeIsDisconnected = false;
                            app.stonehengeViewModelName.model.StonehengePollFailures = 0;
                            app.stonehengeViewModelName.StonehengeSetViewModelData(data);
                        } else {
                            //debugger;
                            setTimeout(function () {
                                app.stonehengeReloadOnError('Events/stonehengeViewModelName = ' + response.status);
                            }, 100);
                        }
                    } catch (error) {
                        setTimeout(function () {
                            app.stonehengeReloadOnError(error);
                        }, 100);
                    }
                    if (continuePolling || app.stonehengeViewModelName.model.StonehengeContinuePolling) {
                        app.stonehengeViewModelName.model.RequestStonehengeEvents(false);
                    }
                })
                .catch(error => {
                    if (app.stonehengeViewModelName.model.StonehengePollEventsActive
                        && app.activeViewModelName === 'stonehengeViewModelName') {
                        //debugger;
                        app.stonehengeViewModelName.model.StonehengePollFailures++;
                        if (app.stonehengeViewModelName.model.StonehengePollFailures > stonehengePollRetries) {
                            app.stonehengeViewModelName.model.StonehengeIsDisconnected = true;
                        }
                    }
                    if (error.status >= 400) {
                        setTimeout(function (app) {
                            app.stonehengeReloadOnError(error);
                        }, 1000);
                    } else {
                        app.stonehengeViewModelName.model.StonehengePollEventsActive = null;
                        if (!app.stonehengeViewModelName.model.StonehengePostActive) {
                            app.stonehengeViewModelName.model.RequestStonehengeEvents(true);
                        }
                    }
                });
        },

        StonehengeGetViewModel: function () {
            app.activeViewModelName = 'stonehengeViewModelName';
            this.StonehengeCancelVmRequests();
            Vue.http.get('ViewModel/stonehengeViewModelName',
                {
                    before(request) {
                        request.headers.set('X-Stonehenge-Id', app.stonehengeViewModelName.model.StonehengeSession);
                    }
                })
                .then(response => {

                    try {
                        app.stonehengeViewModelName.model.StonehengeSession = response.headers.get("X-Stonehenge-Id") || '';
                        try {
                            let data = JSON.parse(response.bodyText);
                            app.stonehengeViewModelName.StonehengeSetViewModelData(data);
                        } catch (error) {
                            if (console && console.log) console.log(error);
                        }
                        app.stonehengeViewModelName.model.StonehengeInitialLoading = false;
                        app.stonehengeViewModelName.model.StonehengeIsLoading = false;
                        app.stonehengeViewModelName.model.StonehengeContinuePolling = true;
                        app.stonehengeViewModelName.RequestStonehengeEvents(true);

                    } catch (e) {
                        console.log("StonehengeGetViewModel EX: " + e)
                        if (stonehengeDebugBuild) debugger;
                    }

                })
                .catch(error => {
                    if (error.status) {
                        //debugger;
                        app.stonehengeViewModelName.model.StonehengeIsDisconnected = true;
                        app.stonehengeReloadOnError(error);
                    }
                });

            if (stonehengeDebugBuild) console.log('stonehengeViewModelName loaded');
        },

        model: {

            StonehengeActive: false,
            StonehengeContinuePolling: false,
            StonehengePollEventsActive: null,
            StonehengePollDelay: stonehengePollDelay,
            StonehengeEventSource: null,
            StonehengeInitialLoading: true,
            StonehengeIsLoading: true,
            StonehengeIsDirty: false,
            StonehengePollFailures: 0,
            StonehengeIsDisconnected: false,
            StonehengePostActive: false,
            StonehengeSession: ''
            //stonehengeProperties

        },

        data: function () {
            if (stonehengeDebugBuild) console.log('stonehengeViewModelName get data');
            //debugger;
            app.stonehengeViewModelName.StonehengeGetViewModel();
            app.stonehengeViewModelName.model.StonehengeActive = true;

            return app.stonehengeViewModelName.model;
        },
        methods: {
            /*commands*/
        }
    };

    stonehengeCancelRequests();
    app.stonehengeViewModelName = vm;
    if (stonehengeDebugBuild) console.log('stonehengeViewModelName created');

    return vm;
};
