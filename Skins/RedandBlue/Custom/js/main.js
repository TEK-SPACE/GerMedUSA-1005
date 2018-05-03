(function ($) {
    "use strict";

$('.news-carousel').owlCarousel({
    loop:true,
    margin:0,
    nav:false,
	dot: true,
    responsive:{
        0:{
            items:1
        },
        600:{
            items:1
        },
        1000:{
            items:1
        }
    }
});

$('.owl-tab').owlCarousel({
    loop:true,
    margin:10,
    nav:true,
	navText : ["<i class='fa fa-angle-left'></i>", "<i class='fa fa-angle-right'></i>" ],
	dot: true,
    responsive:{
        0:{
            items:1
        },
        600:{
            items:1
        },
        1000:{
            items:1
        }
    }
});

	
	
    jQuery(document).ready(function($){

    });


    jQuery(window).load(function(){

        
    });


}(jQuery)); 