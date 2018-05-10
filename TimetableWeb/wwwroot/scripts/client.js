$(function () {

    $("#timetable").fullCalendar({
        weekends: false,
        defaultView: 'agendaWeek',
        minTime: '07:00:00',
        maxTime: '23:00:00',
        header: false,
        allDaySlot:false
    });


    $("#calculateButton").click(function (event) {
        $("#subjectInfo").empty();

        /*$("#subjectList").children().each(function () {

            console.log($(this));

            var subjectFullName = $(this).text().trim();

            var subjectCode = subjectFullName.split(' ')[0];


            $.getJSON('@Url.Action("GetSubjectInfo")?subjectCode=' + subjectCode, function (classInfos) {
                classInfos.forEach(function (classInfo) {
                    $("#subjectInfo").append("<div>" + classInfo.className + ": " + classInfo.classType + "</div>");
                });
            });
        });*/

        var subjectCodes = [];

        $("#subjectList").children().each(function () {
            var subjectFullName = $(this).text().trim();

            var subjectCode = subjectFullName.split(' ')[0];
            subjectCodes.push(subjectCode);
        });
        
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
});