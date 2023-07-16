/*
 * Copyright 2023 Myitian
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#include <memory>
#include "QPDFExc.hh"

struct _qpdf_error
{
	std::shared_ptr<QPDFExc> exc;
};

struct qpdfexc
{
	qpdf_error_code_e error_code;
	const char* filename;
	const char* object;
	qpdf_offset_t offset;
	const char* message;
};

extern "C"
{
	__declspec(dllexport) typedef struct _qpdf_error* qpdf_error;

	__declspec(dllexport) qpdfexc* wrapper_qpdfexc(qpdf_error err)
	{
		qpdfexc* r = (qpdfexc*)malloc(sizeof(qpdfexc));
		if (r)
		{
			r->error_code = err->exc->getErrorCode();
			r->filename = err->exc->getFilename().c_str();
			r->object = err->exc->getObject().c_str();
			r->offset = err->exc->getFilePosition();
			r->message = err->exc->getMessageDetail().c_str();
		}
		return r;
	}

	__declspec(dllexport) void free_qpdfexc(qpdfexc* r)
	{
		free(r);
	}

	__declspec(dllexport) long long wrapper_strlen(const char* str)
	{
		return strlen(str);
	}
}
