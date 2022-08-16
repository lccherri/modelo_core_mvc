const dropPerson      = document.querySelector('.drop-person');
dropPerson.personDetails = {
    displayName: $("#nomeExibicao").val(),
    mail: $("#email").val(),
    personImage: $("#foto").val()
};

const secondPerson = document.querySelector('.my-second-person');
secondPerson.personDetails = {
    displayName: $("#nomeExibicao").val(),
    mail: $("#email").val(),
    personImage: $("#foto").val()
};

const otherPerson = document.querySelector('.my-other-person');
otherPerson.personDetails = {
    displayName: $("#nomeExibicao").val(),
    mail: $("#email").val(),
    personImage: $("#foto").val()
};

