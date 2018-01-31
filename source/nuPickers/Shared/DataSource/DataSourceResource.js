﻿angular.module('umbraco.resources')
    .factory('nuPickers.Shared.DataSource.DataSourceResource',
    ['$http', 'editorState',
        function ($http, editorState) {

            return {

                /**
                 * Get 'editor data items' for a property editor (none or only one of the the optional: typeahead, keys, page params should be set)
                 * @param {Object} - the property editor model
                 * @param {string} - optional typeahead text
                 * @param {Array} - optional array of picked keys
                 * @param {Number} - optional page 
                 * @returns {Object} - a promise to return an http response with data for an array of editorDataItems [{"key":"","label":""},{"key":"","label":""}] and a count when known
                 */
                getEditorDataItems: function (model, typeahead, keys, page) {
                    
                    var currentId = 0;
                    var parentId = 0;
                    var docTypeAlias = "";

                    if (editorState.current) {
                        currentId = editorState.current.id;
                        parentId = editorState.current.parentId;
                        docTypeAlias = editorState.current.contentTypeAlias;
                    }

                    return $http({
                        method: 'POST',
                        url: 'backoffice/nuPickers/DataSourceApi/GetEditorDataItems',
                        params: {
                            'currentId': currentId,
                            'parentId': parentId,
                            'propertyAlias': model.alias,							
                        },
                        data: {
                            'config': model.config,
                            'typeahead': typeahead,
                            'keys': keys,
                            'page': page,
							'docTypeAlias': docTypeAlias
                        }
                    });

                }

            };
        }
    ]);