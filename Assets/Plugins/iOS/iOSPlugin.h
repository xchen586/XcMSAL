#import "UnityAppController.h"
#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import "AuthenticationManager.h"

#include <string>
#include <vector>

extern UIViewController *UnityGetGLViewController();

typedef void (^GetTokenCompletionHandler)(NSString* _Nullable accessToken, NSString * _Nullable errorMessage);

@interface iOSPlugin : NSObject

@end

extern "C"
{
    typedef void (*TokenCallBack)(char * token, char * error);
    
    UIViewController * _GetAppViewController();
    
    bool msalAuthInteractive(const char * clientId, const char * authority, const char * redirect, const char** scopesArray, int scopeSize, const char * clientName, const char * callbackToken, const char * callbackError);
    //public static extern bool msalAuthInteractive(string clientId, string authority, string redirect, string[] scopesArray, int scopeSize, string clientName, IntPtr callback);
}
