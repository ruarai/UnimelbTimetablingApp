﻿$(function () {
    var timetableList = [];
    
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
    $("#slider").slider({
        slide: function (event, ui) {

            $('#timetable').fullCalendar('removeEvents');
            renderTimetable(timetableList[ui.value]);
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

        $('#timetable').fullCalendar('removeEvents');
        $("#timetablesInfo").empty();

        var laterStarts = $('#laterStartsCheckbox').is(':checked');
        var lessDays = $('#lessDaysCheckbox').is(':checked');

        var ajaxURL = '/Home/GetTimetable?codes=' + subjectCodes.join('|') + '&laterStarts=' + laterStarts + '&lessDays=' + lessDays;
        
        $.ajax({
            url: ajaxURL,
            dataType: 'json',
            success: function (timetablesData) {
                $("#calculateButton").attr('disabled', false);
                timetableList = timetablesData;

                renderTimetable(timetablesData[0]);
                $("#progressBar").progressbar('option', 'value', 100);

                if (timetablesData.length === 1)
                    $("#timetablesInfo").append('1 timetable generated');
                else
                    $("#timetablesInfo").append(timetablesData.length + ' timetables generated');

                $("#slider").slider("option", "max", timetablesData.length - 1);
            },
            error: function () {
                $("#calculateButton").attr('disabled', false);
                $("#timetablesInfo").append('Failed to generate timetables.');
            }
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
