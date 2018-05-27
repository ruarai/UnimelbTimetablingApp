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

        updateSubjectInfo();
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


    var previousTimetableRequest = null;
    var previousTimetableRequestTime = new Date().getTime();
    $("#slider").slider({
        slide: function (event, ui) {
            var index = ui.value;

            if (previousTimetableRequestTime + 50 < new Date().getTime()) {
                previousTimetableRequestTime = new Date().getTime();

                previousTimetableRequest = $.ajax({
                    url: 'Home/GetTimetable?index=' + index,
                    dataType: 'json',
                    type: 'GET',
                    beforeSend: function () {
                        if (previousTimetableRequest !== null)
                            previousTimetableRequest.abort();
                    },
                    success: function (timetable) {
                        $('#timetable').fullCalendar('removeEvents');
                        renderTimetable(timetable);
                    }
                });
            }
        }
    });


    $("#calculateButton").click(function (event) {
        $("#calculateButton").attr('disabled', true);
        
        var subjectCodes = getSubjectCodes();

        setStatus('Starting...');

        $('#timetable').fullCalendar('removeEvents');

        var model = {
            subjectCodes: subjectCodes,
            laterStarts: $('#laterStartsCheckbox').is(':checked'),
            lessDays: $('#lessDaysCheckbox').is(':checked')
        };

        $.ajax({
            url: '/Home/BuildTimetable',
            dataType: 'json',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            async: true,
            success: function (timetableModel) {
                $("#calculateButton").attr('disabled', false);

                if (timetableModel.resultStatus === 'failure') {
                    setStatus(timetableModel.resultMessage);
                    return;
                }

                renderTimetable(timetableModel.topTimetable);

                $("#slider").slider("option", "max", timetableModel.numberTimetables - 1);
            },
            error: function () {
                $("#calculateButton").attr('disabled', false);
                setStatus('Failed to generate timetables.');
            }
        });
    });
   

    let connection = new signalR.HubConnectionBuilder()
        .withUrl("/ui")
        .build();

    connection.onClosed = e => {
        console.log('connection to ui lost');
    };
    
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
            updateSubjectInfo();
        });

        div.append(removeButton);
        div.append('<hr/>');

        return div;
    };

    var getSubjectCodes = function () {
        var subjectCodes = [];

        $("#subjectList").children().each(function () {
            var subjectFullName = $(this).text().trim();

            var subjectCode = subjectFullName.split(' ')[0];
            subjectCodes.push(subjectCode);
        });

        return subjectCodes;
    }

    var updateSubjectInfo = function () {
        //disable calculation whilst this happens, otherwise weird stuff can happen with timetable retrieval internally
        $("#calculateButton").attr('disabled', true);
        
        //all filtering/subject info
        var model = {
            subjectCodes: getSubjectCodes()
        };

        $.ajax({
            url: '/Home/UpdateSelectedSubjects',
            dataType: 'json',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (numPermutations) {
                $("#subjectInfo").empty();

                if (numPermutations > 0) {
                    $("#subjectInfo").append(numPermutations.toLocaleString() + ' possible timetables.');
                    $("#calculateButton").attr('disabled', false);
                }
                else {
                    $("#subjectInfo").append('Select some subjects to begin.');
                    //keep disabled until permutations possible
                    $("#calculateButton").attr('disabled', true);
                }

            }
        });
    }


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

            var color = string_to_color(scheduledClass.parentSubject.shortCode);
            var borderColor = string_to_color(scheduledClass.classDescription);

            color = shade(color, (scheduledClass.parentSubject.codeShortDigits % 4 - 2) * 16);

            
            event = {
                start: scheduledClass.timeStart,
                end: scheduledClass.timeEnd,
                title: classLabel,
                backgroundColor: '#' + color,
                borderColor: '#' + borderColor,
                textColor: invertColor(color)
            };

            $("#timetable").fullCalendar('renderEvent', event);
        });
    };

    function invertColor(hex) {
        if (hex.indexOf('#') === 0) {
            hex = hex.slice(1);
        }

        var r = parseInt(hex.slice(0, 2), 16),
            g = parseInt(hex.slice(2, 4), 16),
            b = parseInt(hex.slice(4, 6), 16);
        // http://stackoverflow.com/a/3943023/112731
        return r * 0.299 + g * 0.587 + b * 0.114 > 200 ? '#000000' : '#FFFFFF';
    }


    // Change the darkness or lightness
    var shade = function (color, prc) {
        var num = parseInt(color, 16),
            amt = Math.round(2.55 * prc),
            R = (num >> 16) + amt,
            G = (num >> 8 & 0x00FF) + amt,
            B = (num & 0x0000FF) + amt;
        return (0x1000000 + (R < 255 ? R < 1 ? 0 : R : 255) * 0x10000 +
            (G < 255 ? G < 1 ? 0 : G : 255) * 0x100 +
            (B < 255 ? B < 1 ? 0 : B : 255))
            .toString(16)
            .slice(1);
    };


});

