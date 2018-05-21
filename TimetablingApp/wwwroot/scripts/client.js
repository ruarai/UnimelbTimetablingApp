$(function () {
    var names = subjectList.map(function (item) {
        return item['DisplayName'];
    });

    new Awesomplete('#subjectSearch', {
        list: names
    });

    $("#subjectSearch").on('awesomplete-selectcomplete', function () {
        $("#subjectList").append(buildSubjectListing(this.value));
        $("#subjectSearch").val("");
    });

    $("#timetable").fullCalendar({
        weekends: false,
        defaultView: 'agendaWeek',
        minTime: '08:00:00',
        maxTime: '23:00:00',
        header: false,
        allDaySlot: false,
        displayEventTime: false
    });

    $("#progressBar").progressbar({ value: 0 });


    var previousTimetableRequest = null;
    var previousTimetableRequestTime = new Date().getTime();
    $("#slider").slider({
        slide: function (event, ui) {
            var index = ui.value;

            console.log(index);

            previousTimetableRequest = $.ajax({
                url: 'Home/GetTimetable?index=' + index,
                dataType: 'json',
                type: 'GET',
                beforeSend: function () {
                    if (previousTimetableRequest != null && previousTimetableRequestTime + 50 < new Date().getTime())
                        previousTimetableRequest.abort();
                },
                success: function (timetable) {
                    console.log(timetable);
                    $('#timetable').fullCalendar('removeEvents');
                    renderTimetable(timetable);

                    previousTimetableRequestTime = new Date().getTime();
                }
                });
        }
    });
    

    $("#calculateButton").click(function (event) {
        $("#calculateButton").attr('disabled', true);

        var subjectCodes = [];

        $("#subjectList").children().each(function () {
            var subjectFullName = $(this).text().trim();

            var subjectCode = subjectFullName.split(' ')[0];
            subjectCodes.push(subjectCode);
        });

        setStatus('Starting...');

        $('#timetable').fullCalendar('removeEvents');

        var model = {
            subjectCodes: subjectCodes,
            laterStarts: $('#laterStartsCheckbox').is(':checked'),
            lessDays: $('#lessDaysCheckbox').is(':checked'),
            earliestClassTime: $('#earliestTimeInput').val(),
            latestClassTime: $('#latestTimeInput').val(),
            days: getDays()
        };
        
        $.ajax({
            url: '/Home/BuildTimetable',
            dataType: 'json',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (timetableModel) {
                $("#calculateButton").attr('disabled', false);

                if (timetableModel.resultStatus === 'failure') {
                    setStatus(timetableModel.resultMessage);
                    return;
                }
                
                renderTimetable(timetableModel.topTimetable);
                $("#progressBar").progressbar('option', 'value', 100);

                $("#slider").slider("option", "max", timetableModel.numberTimetables - 1);
            },
            error: function () {
                $("#calculateButton").attr('disabled', false);
                setStatus('Failed to generate timetables.');
            }
        });
    });

    $("#dayList").children().each(function () {
        $(this).click(function () {
            if ($(this).attr('ticked') === 'false')
                $(this).attr('ticked', 'true');
            else
                $(this).attr('ticked', 'false');
        
        });
    });

    let connection = new signalR.HubConnectionBuilder()
        .withUrl("/ui")
        .build();

    connection.onClosed = e => {
        console.log('connection to ui lost');
    };

    connection.on('progress', (message) => {
        $("#progressBar").progressbar('option','value',message*100);
    });
    connection.on('status', (message) => {
        setStatus(message);
    });

    connection.start().catch(err => {
        console.log('connection error');
        console.log(err);
    });

    var buildSubjectListing = function (subjectName) {
        var div = $('<div>' + subjectName + '</div>');

        var removeButton = $('<a class="inlineAction"> (del)</a>');
        removeButton.click(function () {
            this.parentNode.parentNode.removeChild(this.parentNode);
        });

        div.append(removeButton);
        div.append('<hr/>');

        return div;
    };

    var setStatus = function (status) {
        $("#timetablesInfo").empty();
        $("#timetablesInfo").append(status);
    }

    var getDays = function () {
        var days = '';

        $('#dayList').children().each(function () {
            if ($(this).attr('ticked') === 'true')
                days = days + '1';
            else
                days = days + '0';
        });

        return days;
    }

    var renderTimetable = function (timetable) {
        $("#timetable").fullCalendar('gotoDate', timetable.classes[0].timeStart);

        timetable.classes.forEach(function (scheduledClass) {

            var classLabel = scheduledClass.parentSubject.displayName + '\n' +
                scheduledClass.className;

            event = {
                start: scheduledClass.timeStart,
                end: scheduledClass.timeEnd,
                title: classLabel,
                backgroundColor: stringToColour(scheduledClass.parentSubject.code),
                borderColor: stringToColour(scheduledClass.classDescription),
                textColor: invertColor(scheduledClass.parentSubject.code)
            };

            $("#timetable").fullCalendar('renderEvent', event);
        });
    };
    var stringToColour = function (str) {
        var hash = 0;
        for (var i = 0; i < str.length; i++) {
            hash = str.charCodeAt(i) + ((hash << 5) - hash);
        }
        var colour = '#';
        for (var i = 0; i < 3; i++) {
            var value = (hash >> (i * 8)) & 0xFF;
            colour += ('00' + value.toString(16)).substr(-2);
        }
        return colour;
    };

    function invertColor(hex) {
        if (hex.indexOf('#') === 0) {
            hex = hex.slice(1);
        }

        var r = parseInt(hex.slice(0, 2), 16),
            g = parseInt(hex.slice(2, 4), 16),
            b = parseInt(hex.slice(4, 6), 16);
       // http://stackoverflow.com/a/3943023/112731
       return r * 0.299 + g * 0.587 + b * 0.114 > 186 ? '#000000' : '#FFFFFF';
    }
});

