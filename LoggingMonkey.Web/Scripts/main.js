﻿$(document).ready(function () {
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
    
    String.prototype.splice = function (idx, rem, s) {
        return (this.slice(0, idx) + s + this.slice(idx + Math.abs(rem)));
    };

    var thumbnailer = function (text, href) {
        if (!href) {
            return text;
        }

        var uri = new URI(href);
        var hostname = uri.getAuthority();
        var path = uri.getPath();
        var query = uri.getQuery();
        
        switch (hostname) {
            case "i.imgur.com":                
                var extIndex = text.indexOf(".png");
                
                if (extIndex > -1) {
                    text = '<img src="' + text.splice(extIndex, 0, "s") + '"/>';
                }

                break;
                
            case "www.youtube.com":
                var v = query.split('=')[1];

                return '<iframe width="320" height="240" src="http://www.youtube.com/embed/' + v + '" frameborder="0" allowfullscreen></iframe>';
        }

        return '<a href="' + href + '" target="_blank" title="' + href + '">' + text + '</a>';
    };

    $.each($('.log-entry').find('p'), function (index, entry) {
        var text = linkify($(entry).text(), { callback: thumbnailer });
        $(entry).html(text);
    });
});