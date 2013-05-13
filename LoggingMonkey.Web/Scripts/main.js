$(document).ready(function () {
    $('#showOptions').click(function (e) {
        e.preventDefault();

        var $chev = $(this).find('i');
        var $advancedForm = $('#advancedSearch');
        var $displayOptions = $('#displayOptions');

        if ($advancedForm.is(':visible')) {
            $advancedForm.slideUp('fast');
            $displayOptions.slideUp('fast');
            $chev.removeClass('icon-chevron-sign-down');
            $chev.addClass('icon-chevron-sign-right');
        } else {
            $advancedForm.slideDown('fast');
            $displayOptions.slideDown('fast');
            $chev.removeClass('icon-chevron-sign-right');
            $chev.addClass('icon-chevron-sign-down');
        }
    });

    var blank_target_link = function (text, href) {
        return href ? '<a href="' + href + '" target="_blank" title="' + href + '">' + text + '</a>' : text;
    };

    $.each($('.log-entry').find('p'), function (index, entry) {
        var text = linkify($(entry).text(), { callback: blank_target_link });
        $(entry).html(text);
    });
});