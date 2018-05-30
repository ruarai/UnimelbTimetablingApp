$(function () {
    var names = subjectList.map(function (item) {
        return item['DisplayName'];
    });

    new Awesomplete('#subjectSearch', {
        list: names
    });

    var subjectCount = 0;

    $("#subjectSearch").on('awesomplete-selectcomplete', function () {
        var subjectCode = this.value.split(' ')[0];

        if (subjectCount >= 4 || getSubjectCodes().includes(subjectCode)) {
            $("#subjectSearch").val("");
            return;
        }
        subjectCount++;
        
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
        displayEventTime: false,
        contentHeight: 'auto'
    });


    var previousSlideTime = new Date().getTime();
    $("#slider").slider({
        slide: function (event, ui) {
            if (previousSlideTime + 50 > new Date().getTime())
                return;
            previousSlideTime = new Date().getTime();

            var index = ui.value;

            renderTimetable(timetables[index]);
        }
    });

    var classInfos = [];
    var timetables = [];

    $("#calculateButton").click(function (event) {
        $("#calculateButton").attr('disabled', true);
        
        var subjectCodes = getSubjectCodes();

        setStatus('Generating timetables...');

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

                setStatus('Generated ' + timetableModel.timetables.length.toLocaleString() + ' timetables.');

                classInfos = timetableModel.originalClassInfos;
                timetables = timetableModel.timetables;

                renderTimetable(timetableModel.timetables[0]);

                $("#slider").slider("option", "max", timetableModel.timetables.length - 1);
                $("#slider").slider("option", "value", 0);
            },
            error: function () {
                $("#calculateButton").attr('disabled', false);
                setStatus('Failed to generate timetables.');
            }
        });
    });
   
    
    var buildSubjectListing = function (subjectName) {
        var div = $('<div>' + subjectName + '</div>');

        var removeButton = $('<a class="inlineAction"> (del)</a>');
        removeButton.click(function () {
            this.parentNode.parentNode.removeChild(this.parentNode);
            updateSubjectInfo();

            subjectCount--;
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

    var classColors = {
        "Lecture": "039be5",

        "Practical": "d81b60",
        "Seminar": "d81b60",
        "Workshop": "d81b60",
        "Problem-based": "d81b60",
        "Tutorial": "d81b60",
    };


    var renderTimetable = function (timetable) {
        $("#timetable").fullCalendar('removeEvents');

        timetable.classes.forEach(function (compressedClass) {
            classInfo = classInfos.find(function (element) {
                return element.id === compressedClass.id;
            });

            $("#timetable").fullCalendar('gotoDate', compressedClass.start);

            var classLabel = classInfo.parentSubject.displayName + '\n' +
                classInfo.className;

            var color = string_to_color(classInfo.parentSubject.shortCode);
            var borderColor = classColors[classInfo.classDescription];

            if (borderColor == null)
                borderColor = classColors["Practical"];
            
            event = {
                start: compressedClass.start,
                end: compressedClass.end,
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

