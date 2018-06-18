$(function () {
    var names = subjectList.map(function (item) {
        return item['DisplayName'];
    });

    new Awesomplete('#subjectSearch', {
        list: names
    });

    var subjectCount = 0;

    var setStatus;
    var updateSubjectInfo;
    var renderTimetable;
    var setSubjectInfo;



    $("#subjectSearch").on('awesomplete-selectcomplete', function () {
        var subjectCode = this.value.split(' ')[0];

        if (subjectCount >= 5 || getSubjectCodes().includes(subjectCode)) {
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
        contentHeight: 'auto',
        columnHeaderFormat: 'dddd'
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

    var timetables = [];
    var scheduledClasses = [];
    var classInfos = [];

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
            xhr: function () {
                var xhr = new window.XMLHttpRequest();
                xhr.addEventListener("progress", function (evt) {
                    if (evt.loaded > 0) {
                        setStatus('Downloading timetables...');
                    }
                }, false);

                return xhr;
            },
            url: '/Home/BuildTimetable',
            dataType: 'json',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            async: true,
            success: function (timetableModel) {
                $("#calculateButton").attr('disabled', false);

                setStatus('Generated ' + timetableModel.timetablesGenerated.toLocaleString() + ' timetables.');

                timetables = timetableModel.timetables;
                scheduledClasses = timetableModel.allScheduledClasses;
                classInfos = timetableModel.originalClassInfos;

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

    updateSubjectInfo = function () {
        //disable calculation whilst this happens, otherwise weird stuff can happen with timetable retrieval internally
        $("#calculateButton").attr('disabled', true);
        
        //all filtering/subject info
        var model = {
            subjectCodes: getSubjectCodes()
        };

        setSubjectInfo('Fetching timetable info...');

        $.ajax({
            url: '/Home/UpdateSelectedSubjects',
            dataType: 'json',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model),
            success: function (numPermutations) {
                $("#subjectInfo").empty();

                if (numPermutations > 0) {
                    setSubjectInfo(numPermutations.toLocaleString() + ' possible timetables.');
                    $("#calculateButton").attr('disabled', false);
                }
                else {
                    setSubjectInfo('Select some subjects to begin.');
                    //keep disabled until permutations possible
                    $("#calculateButton").attr('disabled', true);
                }

            }
        });
    };


    setStatus = function (status) {
        $("#timetablesInfo").empty();
        $("#timetablesInfo").append(status);
    };

    setSubjectInfo = function(status) {
        $("#subjectInfo").empty();
        $("#subjectInfo").append(status);
    };

    var classColors = {
        "Practical": "ffffff",
        "Seminar": "ffffff",
        "Workshop": "ffffff",
        "Problem-based": "ffffff",
        "Tutorial": "ffffff",
    };


    renderTimetable = function (timetable) {
        $("#timetable").fullCalendar('removeEvents');

        timetable.forEach(function (classID) {
            var scheduledClass = scheduledClasses[classID];
            console.log(scheduledClass);
            var classInfo = classInfos[scheduledClass.classInfoID];
            console.log(classInfo);

            $("#timetable").fullCalendar('gotoDate', scheduledClass.timeStart);

            var classLabel = '';

            if (window.innerWidth < 900) {
                classLabel = classInfo.parentSubject.shortCode + '\n' +
                    classInfo.className;
            }
            else {
                classLabel = classInfo.parentSubject.displayName + '\n' +
                    classInfo.className;
            }

            var color = string_to_color(classInfo.parentSubject.shortCode);
            var borderColor = classColors[classInfo.classDescription];

            if (borderColor == null)
                borderColor = color;
            
            event = {
                start: scheduledClass.timeStart,
                end: scheduledClass.timeEnd,
                title: classLabel,
                backgroundColor: '#' + color,
                borderColor: '#' + borderColor,
                textColor: invertColor(color),
                editable: true
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
});

