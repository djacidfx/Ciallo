//
// Created by Ciao on 2026/1/21.
//

#include "ArrangementObserver.h"
#include "Arrangement2D.h"

void ArrangementObserver::invalid_face(Face_handle f)
{
    if (arr->FaceHandleToID.find(f) != arr->FaceHandleToID.end())
    {
        RID id = arr->FaceHandleToID[f];
        arr->InvalidFaceIDs.append(id);

        arr->FaceHandleToID.erase(f);
        arr->FaceHandleOwner.free(id);
    }
}