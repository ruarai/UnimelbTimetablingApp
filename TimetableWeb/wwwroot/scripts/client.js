$(function () {

    $("#timetable").fullCalendar({
        weekends: false,
        defaultView: 'agendaWeek',
        minTime: '07:00:00',
        maxTime: '23:00:00',
        header: false,
        allDaySlot: false
    });

    $("#progressBar").progressbar({value: 0});


    $("#calculateButton").click(function (event) {
        var subjectCodes = [];

        $("#subjectList").children().each(function () {
            var subjectFullName = $(this).text().trim();

            var subjectCode = subjectFullName.split(' ')[0];
            subjectCodes.push(subjectCode);
        });

        $('#timetable').fullCalendar('removeEvents');

        $.getJSON('/Home/GetTimetable?codes=' + subjectCodes.join('|'), function (timetableData) {
            console.log(timetableData);

            $("#timetable").fullCalendar('gotoDate', timetableData.classes[0].timeStart);

            timetableData.classes.forEach(function (scheduledClass) {
                var classLabel = scheduledClass.className + '\n' + scheduledClass.location;


                event = {
                    start: scheduledClass.timeStart,
                    end: scheduledClass.timeEnd,
                    title: classLabel
                };

                $("#timetable").fullCalendar('renderEvent', event);

                console.log(event);
            });

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
    });

});

